﻿using Microsoft.Extensions.Logging;
using ApplicationServices.Calculator;
using ApplicationServices.Scheduller.Models;
using CronExpressionDescriptor;
using NCrontab;
using ApplicationServices.Scheduller.Jobs;

namespace ApplicationServices.Services
{
    public class CalcNodeService
#nullable disable
    {
        private ILogger<CalcNodeService> Logger { get; set; }
        public static string ConnectionString { get; set; }
        public CalcNode Node { get; set; }
        public bool SchedulledInitialized { get; set; }  = false;
        public bool TriggerInitialized { get; set; } = false;
        private SchedulledCalcsHandler schedulledCalcsHandler;
        private TriggerCalcsHandler triggerCalcsHandler;
        public bool ExistCalcElementByName(string elementName)
        {
            foreach (var element in schedulledCalcsHandler.CalcElements)
            {
                if (element.Name == elementName) return true;
            }
            foreach (var element in triggerCalcsHandler.CalcElements)
            {
                if (element.Name == elementName) return true;
            }
            return false;
        }
        private CalcElement GetCalcElementByName(string elementName)
        {
            foreach (var element in schedulledCalcsHandler.CalcElements)
            {
                if (element.Name == elementName) return element;
            }
            foreach (var element in triggerCalcsHandler.CalcElements)
            {
                if (element.Name == elementName) return element;
            }
            return null;
        }
        public Dictionary<string, string> GetElementCalcAtributesValue(string elementName)
        {
            Dictionary<string, string> atributesValue = new Dictionary<string, string>();
            CalcElement calcElement = GetCalcElementByName(elementName);
            foreach(var atribute in calcElement.Attributes)
            {
                atributesValue.Add(atribute.Name, atribute.Value.ToString());
            }
            return atributesValue;
        }
        public bool HaveSchedulledElements 
        { 
            get
            {
                return schedulledCalcsHandler.CalcElements.Count > 0;
            }
        }
        public bool HaveTriggerElements
        {
            get
            {
                return triggerCalcsHandler.CalcElements.Count > 0;
            }
        }
        public CalcNodeService(ILogger<CalcNodeService> calcServiceLogger, ILogger<CalcsHandler> calcHandlerLogger, string connectionString, CalcNode calcNode)
        {
            try
            {
                Logger = calcServiceLogger;
                ConnectionString = connectionString;
                Node = calcNode;
                
                schedulledCalcsHandler = new SchedulledCalcsHandler(calcHandlerLogger, calcServiceLogger);
                triggerCalcsHandler = new TriggerCalcsHandler(calcHandlerLogger, calcServiceLogger);
            }
            catch (Exception ex)
            {
                Logger.LogError($"При инициализации расчётного узла {Node.SearchAttribute} произошла ошибка :{ex.Message}");
            }
            
        }
        public async Task InitializeModel(CalcMode calcMode)
        {
            if(calcMode == CalcMode.Schedulled)
            {
                string cronDesc = new ExpressionDescriptor(Node.cronExpression, new Options()
                {
                    DayOfWeekStartIndexZero = false,
                    Use24HourTimeFormat = true,
                    Locale = "ru"
                }).GetDescription(DescriptionTypeEnum.FULL);
                Logger.LogInformation($"Узел расчёта: {Node.SearchAttribute} добавлен. Модель элементов: {Node.SearchModel} Шаблон элементов: {Node.SearchTemplate} " +
                        $"с настройками запланированного запуска: {cronDesc}");
            }
            else if (calcMode == CalcMode.Recalc)
            {
                Logger.LogInformation($"Узел расчёта: {Node.SearchAttribute} добавлен. Модель элементов: {Node.SearchModel} Шаблон элементов: {Node.SearchTemplate} " +
                        $"в режиме пересчёта");
            }
            try
            {
                await schedulledCalcsHandler.Initialization(ConnectionString, Node);
                SchedulledInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Узел расчёта: {Node.SearchAttribute}. При инициализации модели по расписанию произошла ошибка: {ex.Message} \r {ex.StackTrace} \r {ex.InnerException}");
            }
            try
            {
                await triggerCalcsHandler.Initialization(ConnectionString, Node, calcMode);
                TriggerInitialized = true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Узел расчёта: {Node.SearchAttribute}. При инициализации модели по триггеру произошла ошибка: {ex.Message} \r {ex.StackTrace} \r {ex.InnerException}");
            }
        }
        public async Task RunSchedulledCalcs(DateTime ts)
        {
            await schedulledCalcsHandler.CalculateSchedulledElements(ts, CalcMode.Schedulled);
        }
        public async Task RunCheckSubscribeDataAbdRunCalcs(ILogger<TriggerCalcsJob> logger)
        {
            if(HaveTriggerElements)
            {
                await triggerCalcsHandler.RunSubscriptionDataGet(logger, CalcMode.Trigger);
            }
        }
        public async Task RecalcNode(DateTime startTime, DateTime endTime)
        {
            Logger.LogInformation($"Узел расчёта: {Node.SearchAttribute}. Запуск пересчёта за период с: {startTime} по: {endTime}");
            Task schedullerRecalcs = RecalcSchedulle(startTime, endTime);
            Task triggerRecalcs = triggerCalcsHandler.RecalcTriggeredElements(startTime, endTime);
            await Task.WhenAll(schedullerRecalcs, triggerRecalcs).WaitAsync(new TimeSpan(10,0,0,0));
            Logger.LogInformation($"Узел расчёта: {Node.SearchAttribute}. Завершён пересчёт за период с: {startTime} по: {endTime}\r\n");
        }
        private async Task RecalcSchedulle(DateTime startTime, DateTime endTime)
        {
            if(!HaveSchedulledElements) { return; }
            var crontabOptions = new CrontabSchedule.ParseOptions();
            crontabOptions.IncludingSeconds = true;
            var schedule = CrontabSchedule.Parse(Node.cronExpression.Replace("?", "*"), crontabOptions);
            DateTime[] tsArr = schedule.GetNextOccurrences(startTime, endTime).ToArray();
            foreach (DateTime ts in tsArr)
            {
                await schedulledCalcsHandler.CalculateSchedulledElements(ts, CalcMode.Recalc);
            }
        }
    }
}
