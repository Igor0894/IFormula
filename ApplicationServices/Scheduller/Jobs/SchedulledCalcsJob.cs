using ApplicationServices.Services;
using Quartz;
using Microsoft.Extensions.Logging;
using ApplicationServices.Scheduller.Models;
using NLog;

namespace ApplicationServices.Scheduller.Jobs
{
    [DisallowConcurrentExecution]
    public class SchedulledCalcsJob : IJob
#nullable disable
    {
        CalcServiceCollector CalcServiceCollector { get; set; }
        private readonly ILogger<SchedulledCalcsJob> logger;
        public SchedulledCalcsJob(ILogger<SchedulledCalcsJob> logger, CalcServiceCollector calcServiceCollector)
        {
            this.logger = logger;
            CalcServiceCollector = calcServiceCollector;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            ScopeContext.PushProperty("calcMode", CalcMode.Schedulled.ToString());
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string name = dataMap.GetString("name");
            CalcService calcService = CalcServiceCollector.GetSchedulledAndTriggeredCalcService(name);
            if (!calcService.SchedulledInitialized)
            {
                /*logger.LogError($"SchedulledCalcsJob по задаче {name} остановлен потому что узел расчёта не инициализирован");
                await context.Scheduler.PauseJob(context.JobDetail.Key);
                return;*/
                logger.LogError($"SchedulledCalcsJob по задаче {name} не выполняется потому что узел расчёта не инициализирован. Попытка инициализации..");
                await calcService.InitializeModel(CalcMode.Schedulled);
            }
            DateTime ts = context.FireTimeUtc.DateTime.ToLocalTime();
            if (calcService.HaveSchedulledElements) { await RunCalc(name, ts, calcService); }
        }
        public async Task RunCalc(string name, DateTime startTs, CalcService calcService)
        {
            logger.LogDebug($"Запущена задача: {name} с меткой времени {startTs}");
            await calcService.RunSchedulledCalcs(startTs);
        }
    }
}
