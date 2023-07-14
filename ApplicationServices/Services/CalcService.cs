using Microsoft.Extensions.Logging;
using TSDBWorkerAPI.Models;
using ApplicationServices.Calculator;
using ApplicationServices.Scheduller.Models;
using CronExpressionDescriptor;
using TSDBWorkerAPI;
using NCrontab;
using ApplicationServices.Scheduller.Jobs;
using System.Xml.Linq;
using Interpreter.Delegates;

namespace ApplicationServices.Services
{
    public class CalcService
#nullable disable
    {
        private ILogger<CalcService> Logger { get; set; }
        public static string ConnectionString { get; set; }
        public CalcNode Node { get; set; }
        public bool SchedulledInitialized { get; set; }  = false;
        public bool TriggerInitialized { get; set; } = false;
        private SchedulledCalcsHandler schedulledCalcsHandler;
        private TriggerCalcsHandler triggerCalcsHandler;
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
        public CalcService(ILogger<CalcService> calcServiceLogger, ILogger<CalcsHandler> calcHandlerLogger, string connectionString, CalcNode calcNode, TsdbClient tsdbWorker)
        {
            try
            {
                Logger = calcServiceLogger;
                ConnectionString = connectionString;
                Node = calcNode;
                tsdbWorker.UpdateSession();
                TSDB.TsdbClient = tsdbWorker;
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
