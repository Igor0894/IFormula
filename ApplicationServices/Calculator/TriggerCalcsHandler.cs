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
using RestSharp;
using System.Net;

namespace ApplicationServices.Calculator
{
    public class TriggerCalcsHandler : CalcsHandler
#nullable disable
    {
        private Guid SubscriptionGuid { get; set; }
        private TimeSpan TimeToLiveSubscription = new TimeSpan(10, 0, 0);
        private DateTime lastSubscribeUpdate;
        List<string> subscriptionTags;
        private Dictionary<string, Value_Type> triggersTypes;
        private DateTime lastSubscriptionDataLoad;
        public TriggerCalcsHandler(ILogger<CalcsHandler> logger, ILogger<CalcService> calcServiceLogger) : base(logger, calcServiceLogger)
        {
            CalcServiceLogger = calcServiceLogger;
        }
        public async Task Initialization(string ConnectionString, CalcNode node, CalcMode calcMode)
        {
            Node = node;
            Prepair(CalcMode.Trigger);
            Server server = new()
            {
                ConnectionString = ConnectionString
            };
            Elements ispElements = server.GetElements(node.SearchModel, "*", node.SearchTemplate, node.SearchAttribute, false);
            foreach (Element ispElement in ispElements)
            {
                Attribute formula = ispElement.Attributes.Properties.Item(node.SearchAttribute);
                bool.TryParse(formula.Value, out bool add);
                if (add)
                {
                    CalcElement element = new()
                    {
                        Id = ispElement.Id,
                        Name = ispElement.Name,
                        Path = ispElement.Path
                    };
                    Attributes children = ispElement.Attributes.Children(formula.Id);
                    element.Initialization(children, ispElement, formula, CalcServiceLogger);
                    if (element.IsTriggerSchedulle) 
                    { 
                        CalcElements.Add(element);
                        TotalCalcAttributes += element.Attributes.Count;
                    }
                    
                }
            }
            if (CalcElements.Count > 0)
            {
                CalcServiceLogger.LogInformation($"Узел расчёта по триггеру: {node.SearchAttribute}. Чтение модели завершено. Элементов: {CalcElements.Count}. Расчётных атрибутов: {TotalCalcAttributes}");
                int totalTags = await GetTagsTypesForOutputTags();
                CalcServiceLogger.LogInformation($"Узел расчёта по триггеру: {node.SearchAttribute}. Считаны типы данных {totalTags} выходных тегов. Входных тегов в формулах: {TotalQueriesInCalcAttributes}\r\n");
                subscriptionTags = new List<string> { };
                foreach (var element in CalcElements)
                {
                    foreach (var item in element.TriggerAttributes)
                    {
                        subscriptionTags.Add(item.OutDataSource.Name);
                    }
                }
                if (calcMode != CalcMode.Recalc) { await GetSubscriptionGuid(); }
            }   
        }
        private async Task GetSubscriptionGuid()
        {
            SubscriptionGuid = await TSDB.TsdbClient.GetFirstSubscriptionGuid(subscriptionTags.ToArray(), TimeToLiveSubscription, subscriptionTags.Count * 100);
            lastSubscribeUpdate = DateTime.Now;
            CalcServiceLogger.LogInformation($"Узел расчёта по триггеру: {Node.SearchAttribute}. Получен Guid подписки {SubscriptionGuid} для {subscriptionTags.Count} тегов подписки.\r\n");
        }
        private async Task UpdateSubscription()
        {
            await TSDB.TsdbClient.UpdateSubscription(SubscriptionGuid, TimeToLiveSubscription);
            lastSubscribeUpdate = DateTime.Now;
            CalcServiceLogger.LogInformation($"Узел расчёта по триггеру: {Node.SearchAttribute}. Обновлён Guid подписки {SubscriptionGuid} для {subscriptionTags.Count} тегов подписки.\r\n");
        }
        private async Task CheckSubscription()
        {
            bool isGoodSubscription = await TSDB.TsdbClient.CheckSubscription(SubscriptionGuid);
            if (isGoodSubscription && (DateTime.Now - lastSubscribeUpdate > (TimeToLiveSubscription - new TimeSpan(0, 10, 0))))
            {
                await UpdateSubscription();
            }
            else if (!isGoodSubscription)
            {
                Guid oldGuid = SubscriptionGuid;
                SubscriptionGuid = await TSDB.TsdbClient.GetSubscriptionGuid(subscriptionTags.ToArray(), TimeToLiveSubscription, subscriptionTags.Count * 100, lastSubscriptionDataLoad);
                CalcServiceLogger.LogInformation($"Узел расчёта по триггеру: {Node.SearchAttribute}. Подписка {oldGuid} обновлённая {lastSubscribeUpdate} с добавленным TimeToLife {TimeToLiveSubscription}" +
                    $"не существует. Получен новый Guid подписки {SubscriptionGuid} для {subscriptionTags.Count} тегов подписки с ArchiveDataStartTimestamp: {lastSubscriptionDataLoad}.\r\n");
                lastSubscribeUpdate = DateTime.Now;
            }
        }
        public async Task RunSubscriptionDataGet(ILogger<TriggerCalcsJob> logger, CalcMode calcMode)
        {
            Prepair(calcMode);
            await CheckSubscription();
            try
            {
                ValuesForWrite = new ConcurrentDictionary<string, List<TSDBSimpleValue>> { };
                var subscriptionDatas = await TSDB.TsdbClient.GetSubscriptionData(SubscriptionGuid, subscriptionTags.Count * 10);
                lastSubscriptionDataLoad = DateTime.Now;
                if (subscriptionDatas.Count > 0 && subscriptionDatas.Values.FirstOrDefault().Count == 0) { return; }
                Parallel.ForEach(subscriptionDatas, data =>
                {
                    foreach (var element in CalcElements)
                    {
                        if (element.TriggerAttributes.Any(a => a.OutDataSource.Name == data.Key))
                        {
                            CalculateTriggeredElements(element, data.Value, logger);
                        }
                    }
                });
                log.Append(string.Join("", calcLog));
                if (ValuesForWrite.Count > 0)
                {
                    log.AppendFormat("Расчет формул успешно завершен. Начинается запись выходных значений в теги TSDB.\r\n");
                    await WriteTsdbValues(ValuesForWrite);
                }
                timer.Stop();
                TimeSpan time = timer.Elapsed;
                log.AppendFormat("Расчет успешно завершен: время выполнения {0}", time.ToString(@"m\:ss\.fff"));
                SendLog(log.ToString(), false);
            }
            catch (Exception e)
            {
                log.AppendFormat("Ошибка при итеарции расчета.Будет выполнена попытка получения новой подписки:\n{0}\n{1}\n{2}\r\n", e.Message, e.StackTrace, e.InnerException);
                await GetSubscriptionGuid();
                SendLog(log.ToString(), true);
            }
        }
        private void CalculateTriggeredElements(CalcElement element, List<TSDBValue> values, ILogger<TriggerCalcsJob> logger)
        {
            var sortedValuesByTime = values.GroupBy(p => p.Timestamp).Select(g => g.First()).OrderBy(t => t.Timestamp).ToList();
            foreach (TSDBValue value in sortedValuesByTime)
            {
                logger.LogDebug($"Запущена задача расчёта элемента: {element.Name} по приходу значения: {value}");
                CalculateElement(element, value.Timestamp);
            }
        }
        public async Task RecalcTriggeredElements(DateTime startTime, DateTime endTime)
        {
            if (CalcElements.Count == 0) { return; }
            Prepair(CalcMode.Recalc);
            try
            {
                triggersTypes = await GetTagsTypesForTriggerTags(subscriptionTags);
                CalcServiceLogger.LogInformation($"Считаны типы данных для {triggersTypes.Count} триггерных тегов");
                await Parallel.ForEachAsync(subscriptionTags, async (tag, token) =>
                {
                    await TakeTriggersValuesAndRunCalcsForTag(startTime, endTime, tag);
                });
                log.Append(string.Join("", calcLog));
                if (ValuesForWrite.Count > 0)
                {
                    log.AppendFormat("Расчет формул успешно завершен. Начинается запись выходных значений в теги TSDB.\r\n");
                    await WriteTsdbValues(ValuesForWrite);
                }
                timer.Stop();
                TimeSpan time = timer.Elapsed;
                log.AppendFormat("Расчет успешно завершен: время выполнения {0}", time.ToString(@"m\:ss\.fff"));
                SendLog(log.ToString(), false);
            }
            catch (Exception e)
            {
                log.AppendFormat("Ошибка при итеарции расчета:\n{0}\n{1}\n{2}\r\n", e.Message, e.StackTrace, e.InnerException);
                SendLog(log.ToString(), true);
            }
        }
        private async Task TakeTriggersValuesAndRunCalcsForTag(DateTime startTime, DateTime endTime, string tagName)
        {
            List<TSDBValue> values = new List<TSDBValue>();
            if (triggersTypes[tagName].ToString() == Value_Type.DOUBLE.ToString())
            {
                values = await TSDB.TsdbClient.GetTakeFrameByTag<double>(tagName, startTime, endTime);
            }
            else if (triggersTypes[tagName].ToString() == Value_Type.LONG.ToString())
            {
                values = await TSDB.TsdbClient.GetTakeFrameByTag<long>(tagName, startTime, endTime);
            }
            else if (triggersTypes[tagName].ToString() == Value_Type.SET.ToString())
            {
                values = await TSDB.TsdbClient.GetTakeFrameByTag<long>(tagName, startTime, endTime);
            }
            else if (triggersTypes[tagName].ToString() == Value_Type.STRING.ToString())
            {
                values = await TSDB.TsdbClient.GetTakeFrameByTag<string>(tagName, startTime, endTime);
            }
            var sortedValues = values.GroupBy(p => p.Timestamp).Select(g => g.First()).OrderBy(t => t.Timestamp).ToList();
            foreach (var element in CalcElements)
            {
                if (element.TriggerAttributes.Any(a => a.OutDataSource.Name == tagName))
                {
                    foreach (TSDBValue value in sortedValues)
                    {
                        lock (logMutex)
                        {
                            calcLog.Add($"\r\nЗапуск расчета {value.Timestamp}\r\n");
                        }
                        CalculateElement(element, value.Timestamp);
                    }
                }
            }
        }
    }
}
