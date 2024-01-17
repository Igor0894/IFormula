using ApplicationServices.Services;
using ISP.SDK;
using Microsoft.Extensions.Logging;
using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Interpreter.Delegates;
using TSDBWorkerAPI;
using ApplicationServices.Scheduller.Models;
using ISP.SDK.IspObjects;
using Attribute = ISP.SDK.IspObjects.Attribute;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;
using Interpreter;

namespace ApplicationServices.Calculator
{
    public class CalcsHandler
#nullable disable
    {
        internal ILogger<CalcsHandler> Logger { get; set; }
        internal ILogger<CalcNodeService> CalcServiceLogger { get; set; }
        public CalcNode Node { get; set; }
        public List<CalcElement> CalcElements { get; set; } = new List<CalcElement>();
        internal Dictionary<string, Value_Type> OutputTagsTypes { get; set; }
        internal Mutex logMutex = new Mutex();
        internal ConcurrentDictionary<string, List<TSDBSimpleValue>> ValuesForWrite;
        public int TotalCalcAttributes { get; set; } = 0;
        public int TotalQueriesInCalcAttributes { get; set; } = 0;
        internal StringBuilder log = new();
        internal List<string> calcLog;
        internal Stopwatch timer;
        public CalcsHandler(ILogger<CalcsHandler> logger, ILogger<CalcNodeService> calcServiceLogger)
        {
            Logger = logger;
            CalcServiceLogger = calcServiceLogger;
        }
        public async Task TestElementInitialization(string ConnectionString, Guid elementId, CalcNode[] nodes)
        {
            Server server = new()
            {
                ConnectionString = ConnectionString
            };
            Element ispElement = server.GetElement(elementId);
            if(ispElement.Id == null) { throw new Exception($"Не найден элемент с Guid: {elementId}."); }
            if (ispElement.Attributes.Properties.Where(atr => nodes.Select(node => node.SearchAttribute).ToArray().Contains(atr.Name)).Count() == 0) { throw new Exception($"В элементе с Guid: {elementId} не найден атрибут из переченя Nodes.json."); }
            string searchAttribute = ispElement.Attributes.Properties.Where(atr => nodes.Select(node => node.SearchAttribute).ToArray().Contains(atr.Name)).FirstOrDefault().Name;
            Node = nodes.Where(node => node.SearchAttribute == searchAttribute).FirstOrDefault();
            Prepair(CalcMode.Test);
            Attribute formula = ispElement.Attributes.Properties.Item(searchAttribute);
            CalcElement element = new()
            {
                Id = ispElement.Id,
                Name = ispElement.Name,
                Path = ispElement.Path
            };
            Attributes children = ispElement.Attributes.Children(formula.Id);
            element.Initialize(children, ispElement, formula, CalcServiceLogger);
            if (element.SuccessSorted)
            {
                CalcElements.Add(element);
                TotalCalcAttributes += element.CalcAttributes.Count;
            }
            else
            {
                throw new Exception($"Имеется зависимость расчётных атрибутов друг от друга. Невозможно отсортировать элемент: {element.Name}");
            }
            Logger.LogInformation($"Тестируемый элемент: {element.Name} инициализирован. Расчётных атрибутов: {TotalCalcAttributes}");
            int totalTags = await GetTagsTypesForOutputTags();
            Logger.LogInformation($"Тестируемый элемент: {element.Name}. Считаны типы данных {totalTags} выходных тегов. Входных тегов в формулах: {TotalQueriesInCalcAttributes}\r\n");
        }
        internal async Task<int> GetTagsTypesForOutputTags()
        {
            Dictionary<string, Value_Type>  tags = new Dictionary<string, Value_Type>();
            foreach(var element in CalcElements)
            {
                foreach(var item in element.CalcAttributes)
                {
                    if (item.OutDataSource.Type == AttributeValueType.TSDB && !string.IsNullOrEmpty(item.OutDataSource.Name) && !tags.ContainsKey(item.OutDataSource.Name))
                    {
                        try
                        {
                            string typeString = await TSDB.TsdbClient.GetMetaAttribute(item.OutDataSource.Name, "Value_Type");
                            Value_Type type = (Value_Type)Enum.Parse(typeof(Value_Type), typeString);
                            tags.Add(item.OutDataSource.Name, type);
                        }
                        catch(Exception ex)
                        {
                            CalcServiceLogger.LogError($"Ошибка при получении типа тега {item.OutDataSource.Name}: {ex.Message}");
                        }
                    }
                    TotalQueriesInCalcAttributes += Regex.Matches(item.Expression, "Tag").Count;
                }
            }
            OutputTagsTypes = tags;
            return tags.Count;
        }
        internal async Task<Dictionary<string, Value_Type>> GetTagsTypesForTriggerTags(List<string> subscriptionTags)
        {
            Dictionary<string, Value_Type> tags = new Dictionary<string, Value_Type>();
            foreach (var tag in subscriptionTags)
            {
                if (tags.ContainsKey(tag)) continue;
                try
                {
                    string typeString = await TSDB.TsdbClient.GetMetaAttribute(tag, "Value_Type");
                    Value_Type type = (Value_Type)Enum.Parse(typeof(Value_Type), typeString);
                    tags.Add(tag, type);
                }
                catch (Exception ex)
                {
                    CalcServiceLogger.LogError($"Ошибка при получении типа тега {tag}: {ex.Message}");
                }
            }
            return tags;
        }
        internal void CalculateElement(CalcElement element, DateTime ts)
        {
            BlockingCollection<string> innerLog = new()
            {
                $"---------------------Элемент {element.Name} запускается на метку времени {ts}\n"
            };
            element.Interpreter.CurrentTime = ts;
            foreach (List<CalcAttribute> calcItems in element.Queue)
            {
                CalculateQueue(element, calcItems, innerLog, ts);
            }
            lock (logMutex)
            {
                innerLog.Add("\r\n");
                calcLog.AddRange(innerLog);
            }
            
        }
        internal void CalculateTestElements(DateTime ts)
        {
            if (CalcElements.Count == 0) { return; }
            Prepair(CalcMode.Test);
            try
            {
                log.AppendFormat("Запуск расчета: {0}\r\n", ts);
                Parallel.ForEach(CalcElements, e =>
                {
                    CalculateElement(e, ts);
                });
                log.Append(string.Join("", calcLog));
                if (ValuesForWrite.Count > 0)
                {
                    log.AppendFormat($"Тестовый расчет формул успешно завершен. Запись {ValuesForWrite.Count} выходных значений в теги TSDB производится не будет.\r\n");
                }
                timer.Stop();
                TimeSpan time = timer.Elapsed;
                log.AppendFormat("Расчет успешно завершен: время выполнения {0}", time.ToString(@"m\:ss\.fff"));
                SendLog(log.ToString(), false);
            }
            catch (Exception e)
            {
                log.AppendFormat("Ошибка при итерации тестового расчета по расписанию:\n{0}\n{1}\n{2}\r\n", e.Message, e.StackTrace, e.InnerException);
                SendLog(log.ToString(), true);
            }
        }
        private void CalculateQueue(CalcElement element, List<CalcAttribute> calcAttributes, BlockingCollection<string> innerLog, DateTime ts)
        {
            Parallel.For(0, calcAttributes.Count, (i, state) =>
            {
                CalculateAttribute(calcAttributes.ToArray()[i], state, element, innerLog, ts);
            });
            foreach (CalcAttribute attribute in calcAttributes)
            {
                if (!string.IsNullOrEmpty(attribute.Variable) && !(attribute.Value is null))
                {
                    element.Interpreter.SetVariable(attribute.Variable, attribute.Value);
                }
            }
        }
        private void CalculateAttribute(CalcAttribute attribute, ParallelLoopState state, CalcElement element, BlockingCollection<string> innerLog, DateTime calcTs)
        {
            try
            {
                attribute.Value = element.Interpreter.Eval(attribute.Expression.ToLower());
                string logValue = attribute.Value.ToString() == double.MinValue.ToString() ? "NoOutput" : attribute.Value.ToString();
                string log = "[Очередь №" + attribute.Order + "] Атрибут: " + attribute.Name + " | Функция: " + attribute.Expression + " | Результат: " + logValue;
                DateTime ts;
                if (attribute.Value.ToString() == (double.MinValue + 1).ToString()) state.Break(); //Exit
                if (!string.IsNullOrEmpty(attribute.OutDataSource.Name) && attribute.Value.ToString() != double.MinValue.ToString()) //NoOutput
                {
                    log += " | Выход: " + attribute.OutDataSource.Name;
                    if (DateTime.TryParse(element.Interpreter.Eval(attribute.OutDataSource.Time).ToString(), out ts))
                    {
                        if (attribute.OutDataSource.Type == AttributeValueType.TSDB)
                        {
                            Dictionary<string, List<TSDBSimpleValue>> values = new Dictionary<string, List<TSDBSimpleValue>> { };
                            TSDBSimpleValue valPoint = new TSDBSimpleValue()
                            {
                                Value = attribute.Value.ToString(),
                                TimestampUTC = ts.ToUniversalTime()
                            };
                            AddCalcedValuesToDict(attribute, valPoint);
                        }
                        else if (attribute.OutDataSource.Type == AttributeValueType.SQL)
                        {
                            element.Sql.UpdateSQLValue(Guid.Parse(attribute.OutDataSource.Id), attribute.Value.ToString(), ts);
                        }
                    }
                    else throw new Exception("Неверный формат даты записи значения метки времени");
                    log += " | Время записи: " + ts;
                }
                else if (!string.IsNullOrEmpty(attribute.OutReWriteDataSource.Name) && attribute.Value.ToString() != double.MinValue.ToString()) //NoReOutput
                {
                    log += " | Перезапись: " + attribute.OutReWriteDataSource.Name;
                    if (attribute.OutReWriteDataSource.Type == AttributeValueType.SQL)
                    {
                        element.Sql.UpdateLastSQLValue(Guid.Parse(attribute.OutReWriteDataSource.Id), attribute.Value.ToString(), calcTs);
                    }
                }
                log += "\n";
                bool addedToLog = false;
                while (!addedToLog)
                {
                    addedToLog = innerLog.TryAdd(log);
                }
            }
            catch (Exception ex)
            {
                innerLog.Add("ОШИБКА! " + "Атрибут: " + attribute.Name + " | Формула: " + attribute.Expression + ". " + ex.Message + "\n");
                attribute.Value = "Ошибка в формуле: " + attribute.Expression + ". " + ex.Message;
            }
        }
        private void AddCalcedValuesToDict(CalcAttribute attribute, TSDBSimpleValue valPoint)
        {
            if (!ValuesForWrite.ContainsKey(attribute.OutDataSource.Name))
            {
                List<TSDBSimpleValue> newValues = new List<TSDBSimpleValue>() { valPoint };
                ValuesForWrite.TryAdd(attribute.OutDataSource.Name, newValues);
            }
            else
            {
                ValuesForWrite[attribute.OutDataSource.Name].Add(valPoint);
            }
        }
        internal async Task WriteTsdbValues(ConcurrentDictionary<string, List<TSDBSimpleValue>> allValues)
        {
            ConcurrentDictionary<string, List<TSDBSimpleValue>> valuesDouble = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
            ConcurrentDictionary<string, List<TSDBSimpleValue>> valuesLong = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
            ConcurrentDictionary<string, List<TSDBSimpleValue>> valuesFloat = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
            ConcurrentDictionary<string, List<TSDBSimpleValue>> valuesString = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
            Parallel.ForEach(allValues, KeyValuePair =>
            {
                try
                {
                    string tag = KeyValuePair.Key;
                    if (!OutputTagsTypes.ContainsKey(tag)) 
                    { 
                        log.AppendFormat($"Не загружен тип тега [{tag}]. Повторим попытку..\r\n");
                        try
                        {
                            string typeString = TSDB.TsdbClient.GetMetaAttribute(tag, "Value_Type").Result;
                            Value_Type type = (Value_Type)Enum.Parse(typeof(Value_Type), typeString);
                            OutputTagsTypes.Add(tag, type);
                        }
                        catch (Exception ex)
                        {
                            CalcServiceLogger.LogError($"Ошибка при получении типа тега {tag}: {ex.Message}");
                            return;
                        }
                    }
                    Value_Type tagType = OutputTagsTypes[tag];
                    switch (tagType)
                    {
                        case Value_Type.DOUBLE:
                            WriteValuesToDictionary<double>(tag, valuesDouble, log);
                            break;
                        case Value_Type.SET:
                            WriteValuesToDictionary<long>(tag, valuesLong, log);
                            break;
                        case Value_Type.LONG:
                            WriteValuesToDictionary<long>(tag, valuesLong, log);
                            break;
                        case Value_Type.FLOAT:
                            WriteValuesToDictionary<float>(tag, valuesFloat, log);
                            break;
                        default:
                            WriteValuesToDictionary<string>(tag, valuesString, log);
                            break;
                    }
                }
                catch(Exception ex)
                {
                    log.AppendFormat($"При подготовки записи данных в тег произошла ошибка: {ex.Message}\r\n");
                }
            });
            try
            {
                Dictionary<Type, ConcurrentDictionary<string, List<TSDBSimpleValue>>> values = new()
            {
                { typeof(double), valuesDouble },
                { typeof(long), valuesLong },
                { typeof(float), valuesFloat },
                { typeof(string), valuesString }
            };
                await Parallel.ForEachAsync(values, async (value, token) =>
                {
                    if (value.Key == typeof(double))
                    {
                        await TSDB.UpdateValues<double>(valuesDouble);
                    }
                    else if (value.Key == typeof(long))
                    {
                        await TSDB.UpdateValues<long>(valuesLong);
                    }
                    else if (value.Key == typeof(float))
                    {
                        await TSDB.UpdateValues<float>(valuesFloat);
                    }
                    else
                    {
                        await TSDB.UpdateValues<string>(valuesString);
                    }
                });
            }
            catch (Exception ex)
            {
                log.Append($"При записи данных в теги произошла ошибка: {ex.Message}\r\n{ex.InnerException}\r\n{ex.StackTrace}\r\n");
            }
        }
        private void WriteValuesToDictionary<T>(string tagName, ConcurrentDictionary<string, List<TSDBSimpleValue>> valuesDict, StringBuilder log)
        {
            if (!valuesDict.ContainsKey(tagName))
            {
                List<TSDBSimpleValue> newValues = new List<TSDBSimpleValue>() { };
                valuesDict.TryAdd(tagName, newValues);
            }
            Parallel.ForEach(ValuesForWrite[tagName], value =>
            {
                try
                {
                    if (typeof(T) == typeof(double))
                    {
                        bool parsed = double.TryParse(value.Value.ToString(), out double resultDouble);
                        if(!parsed || double.IsNaN(resultDouble)) 
                        {
                            value.Quality = Quality.bad;
                        }
                        value.Value = resultDouble;
                    }
                    if (typeof(T) == typeof(long))
                    {
                        bool parsed = long.TryParse(value.Value.ToString(), out long resultLong);
                        if (!parsed)
                        {
                            value.Quality = Quality.bad;
                        }
                        value.Value = resultLong;
                    }
                    if (typeof(T) == typeof(float))
                    {
                        bool parsed = float.TryParse(value.Value.ToString(), out float resultFloat);
                        if (!parsed || float.IsNaN(resultFloat))
                        {
                            value.Quality = Quality.bad;
                        }
                        value.Value = resultFloat;
                    }
                    valuesDict[tagName].Add(value);
                }
                catch (Exception ex)
                {
                    log.Append($"Ошибка записи значения в тег {tagName} :{ex.Message}\r\n");
                }
            });
        }
        internal void SendLog(string text, bool error)
        {
            if (error)
                Logger.LogError(text + "\r");
            else
                Logger.LogInformation(text + "\r");
        }
        internal void Prepair(CalcMode calcMode)
        {
            ScopeContext.PushProperty("node", Node.SearchAttribute);
            ScopeContext.PushProperty("calcMode", calcMode.ToString());
            ValuesForWrite = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
            calcLog = new();
            timer = new();
            timer.Start();
            log.Clear();
        }
    }
}
