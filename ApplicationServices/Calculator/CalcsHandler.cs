using ApplicationServices.Services;
using ISP.SDK;
using Microsoft.Extensions.Logging;
using NLog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Interpreter.Delegates;
using TSDBWorkerAPI;
using TSDBWorkerAPI.Models;
using ApplicationServices.Scheduller.Models;
using ISP.SDK.IspObjects;
using Attribute = ISP.SDK.IspObjects.Attribute;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Quartz.Logging;
using System.IO;
using System;
using ApplicationServices.Scheduller.Jobs;
using Newtonsoft.Json.Linq;

namespace ApplicationServices.Calculator
{
    public class CalcsHandler
#nullable disable
    {
        internal ILogger<CalcsHandler> Logger { get; set; }
        internal ILogger<CalcService> CalcServiceLogger { get; set; }
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
        public CalcsHandler(ILogger<CalcsHandler> logger, ILogger<CalcService> calcServiceLogger)
        {
            Logger = logger;
            CalcServiceLogger = calcServiceLogger;
        }
        internal async Task<int> GetTagsTypesForOutputTags()
        {
            Dictionary<string, Value_Type>  tags = new Dictionary<string, Value_Type>();
            foreach(var element in CalcElements)
            {
                foreach(var item in element.Attributes)
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
                $"---------------------Элемент {element.Name} с меткой времени {ts}\n"
            };
            element.Interpreter.CurrentTime = ts;
            foreach (List<CalcAttribute> calcItems in element.Queue)
            {
                CalculateQueue(element, calcItems, innerLog, ts);
            }
            lock (logMutex)
            {
                calcLog.AddRange(innerLog);
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
                string log = "[Очередь №" + attribute.Order + "] Атрибут: " + attribute.Name + " | Функция: " + attribute.Expression + " | Результат: " + attribute.Value;
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
                                Value = attribute.Value,
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
                innerLog.Add("ОШИБКА! " + "Атрибут: " + attribute.Name + " | Функция: " + attribute.Expression + ". " + ex.Message + "\n");
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
                    if (typeof(T) == typeof(double) && !double.TryParse(value.Value.ToString(), out double resultDouble))
                    {
                        value.Value = resultDouble;
                        value.Quality = Quality.bad;
                        //throw new Exception($"Неверный тип данных значения для double: {value.Value}");
                    }
                    if (typeof(T) == typeof(long) && !long.TryParse(value.Value.ToString(), out long resultLong))
                    {
                        value.Value = resultLong;
                        value.Quality = Quality.bad;
                        //throw new Exception($"Неверный тип данных значения для long (SET): {value.Value}");
                    }
                    if (typeof(T) == typeof(float) && !float.TryParse(value.Value.ToString(), out float resultFloat))
                    {
                        value.Value = resultFloat;
                        value.Quality = Quality.bad;
                        //throw new Exception($"Неверный тип данных значения для float: {value.Value}");
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
