using ApplicationServices.Services;
using Quartz;
using Microsoft.Extensions.Logging;
using ApplicationServices.Scheduller.Models;
using NLog;

namespace ApplicationServices.Scheduller.Jobs
{
    [DisallowConcurrentExecution]
    public class TriggerCalcsJob : IJob
#nullable disable
    {
        CalcServiceCollector CalcServiceCollector { get; set; }
        private readonly ILogger<TriggerCalcsJob> logger;
        public TriggerCalcsJob(ILogger<TriggerCalcsJob> logger, CalcServiceCollector calcServiceCollector)
        {
            this.logger = logger;
            CalcServiceCollector = calcServiceCollector;
        }
        public async Task Execute(IJobExecutionContext context)
        {
            ScopeContext.PushProperty("calcMode", CalcMode.Trigger.ToString());
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            string name = dataMap.GetString("name");
            CalcService calcService = CalcServiceCollector.GetSchedulledAndTriggeredCalcService(name);
            if (!calcService.TriggerInitialized)
            {
                logger.LogError($"TriggerCalcsJob по задаче {name} пропущен потому что узел расчёта не инициализирован");
                //await context.Scheduler.PauseJob(context.JobDetail.Key);
                return;
            }
            /*DateTime ts = context.FireTimeUtc.DateTime.ToLocalTime();
            logger.LogDebug($"Проверки новых значений триггеров узла расчётов: {name}");*/
            await calcService.RunCheckSubscribeDataAbdRunCalcs(logger);
        }
    }
}
