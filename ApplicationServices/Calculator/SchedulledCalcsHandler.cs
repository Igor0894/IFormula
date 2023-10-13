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

namespace ApplicationServices.Calculator
{
    public class SchedulledCalcsHandler : CalcsHandler
#nullable disable
    {
        public SchedulledCalcsHandler(ILogger<CalcsHandler> logger, ILogger<CalcNodeService> calcServiceLogger) : base(logger, calcServiceLogger)
        {
            
        }
        public async Task Initialization(string ConnectionString, CalcNode node)
        {
            Node = node;
            Prepair(CalcMode.Schedulled);
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
                    element.Initialize(children, ispElement, formula, CalcServiceLogger);
                    if (!element.IsTriggerSchedulle && element.SuccessSorted)
                    {
                        CalcElements.Add(element);
                        TotalCalcAttributes += element.Attributes.Count;
                    }
                    if (element.SuccessSorted)
                    {
                        TotalCalcAttributes += element.Attributes.Count;
                    }
                    else
                    {
                        CalcServiceLogger.LogError($"Имеется зависимость расчётных атрибутов друг от друга. Невозможно отсортировать элемент: {element.Name}");
                    }
                }
            }
            if (CalcElements.Count > 0)
            {
                CalcServiceLogger.LogInformation($"Узел расчёта по расписанию: {node.SearchAttribute}. Чтение модели завершено. Элементов: {CalcElements.Count}. Расчётных атрибутов: {TotalCalcAttributes}");
                int totalTags = await GetTagsTypesForOutputTags();
                CalcServiceLogger.LogInformation($"Узел расчёта по расписанию: {node.SearchAttribute}. Считаны типы данных {totalTags} выходных тегов. Входных тегов в формулах: {TotalQueriesInCalcAttributes}\r\n");
            }
        }
        public async Task CalculateSchedulledElements(DateTime ts, CalcMode calcMode)
        {
            if (CalcElements.Count == 0) { return; }
            Prepair(calcMode);
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
                log.AppendFormat("Ошибка при итеарции расчета по расписанию:\n{0}\n{1}\n{2}\r\n", e.Message, e.StackTrace, e.InnerException);
                SendLog(log.ToString(), true);
            }
        }
    }
}
