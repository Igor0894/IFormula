using ApplicationServices.Scheduller.Jobs;
using ApplicationServices.Scheduller.Models;
using Quartz.Impl;
using Quartz;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using System.Threading;
using System.Xml.Linq;

namespace ApplicationServices.Services
{
    public class ManageNodeService
#nullable disable
    {
        private IJobFactory JobFactory { get; set; }
        private ILogger<CalcService> Logger { get; set; }
        private CalcServiceCollector CalcServiceCollector { get; set; }
        private IScheduler scheduler;
        private SemaphoreSlim recalcSemaphore = new SemaphoreSlim(1, 1);
        public ManageNodeService(IJobFactory jobFactory, ILogger<CalcService> logger, CalcServiceCollector calcServiceCollector)
        {
            JobFactory = jobFactory;
            Logger = logger;
            CalcServiceCollector = calcServiceCollector;
        }
        public async Task<IResult> AddOrStartNode(string name)
        {
            await TryDeleteJobs(name);
            await StartJob(name);
            return Results.Ok();
        }
        public async Task<IResult> StopNode(string name)
        {
            if (await TryDeleteJobs(name))
            {
                Logger.LogInformation($"Остановлен узел расчёта: {name}");
            }
            else
            {
                throw new Exception($"Указанный узел расчёта не запущен: {name}");
            }
            return Results.Ok();
        }
        private async Task<bool> TryDeleteJobs(string name)
        {
            bool stopped = false;
            scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            scheduler.JobFactory = JobFactory;
            var groupMatcher = GroupMatcher<TriggerKey>.GroupContains("Nodes");
            var executingTriggers = await scheduler.GetTriggerKeys(groupMatcher);
            if (executingTriggers.Any(j => j.Name == name))
            {
                await scheduler.DeleteJob(new JobKey(name + " schedulle"));
                stopped = true;
            }
            groupMatcher = GroupMatcher<TriggerKey>.GroupContains("TriggerNodes");
            executingTriggers = await scheduler.GetTriggerKeys(groupMatcher);
            if (executingTriggers.Any(j => j.Name == name))
            {
                await scheduler.DeleteJob(new JobKey(name + " trigger"));
                stopped = true;
            }
            CalcServiceCollector.DeleteCalcService(name);
            return stopped;
        }
        public async Task<IResult> RecalcNode(string name, string startTimeLocal, string endTimeLocal)
        {
            if (!DateTime.TryParse(startTimeLocal, out DateTime start))
            {
                throw new Exception($"Не распознана метка времени: {startTimeLocal}");
            }
            if (!DateTime.TryParse(endTimeLocal, out DateTime end))
            {
                throw new Exception($"Не распознана метка времени: {endTimeLocal}");
            }
            await Task.Run(() => StartRecalcNode(name, start, end));
            return Results.Ok();
        }
        private async Task StartRecalcNode(string name, DateTime start, DateTime end)
        {
            await recalcSemaphore.WaitAsync();
            CalcService calcService = await GetCalcService(name);
            calcService.RecalcNode(start, end);
            recalcSemaphore.Release();
        }
        private async Task StartJob(string name)
        {
            CalcNode node = await ReadNode(name);
            await scheduler.Start();
            JobForSchedule<SchedulledCalcsJob> job = new(node, "Nodes");
            try
            {
                await scheduler.ScheduleJob(job.JobDetail, job.Trigger);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка запуска узла {node.SearchAttribute} с режимом запуска по расписанию: {ex.Message}");
            }
            JobForTrigger<TriggerCalcsJob> triggerJob = new(node, "TriggerNodes");
            try
            {
                await scheduler.ScheduleJob(triggerJob.JobDetail, triggerJob.Trigger);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка запуска узла {node.SearchAttribute} с режимом запуска по триггеру: {ex.Message}");
            }
        }
        private async Task<CalcNode> ReadNode(string name)
        {
            CalcNode[] Nodes;
            CalcNode node = default!;
            using (FileStream fs = new("Nodes.json", FileMode.OpenOrCreate))
            {
                if (fs != null)
                {
                    Nodes = JsonSerializer.Deserialize<CalcNode[]>(fs)!;
                    if (!Nodes.Any(n => n.SearchAttribute == name))
                    {
                        throw new Exception("Указанный узел расчёта отсутствует в конфигурационном файле");
                    }
                    node = Nodes.Where(n => n.SearchAttribute == name).FirstOrDefault()!;
                }
                else
                {
                    Logger.LogError($"В файле Nodes.json XML некорректного формата");
                }
            }
            await CalcServiceCollector.AddCalcService(node);
            return node;
        }
        private async Task<CalcService> GetCalcService(string name)
        {
            CalcNode[] Nodes;
            CalcNode node = default!;
            using (FileStream fs = new("Nodes.json", FileMode.OpenOrCreate))
            {
                if (fs != null)
                {
                    Nodes = JsonSerializer.Deserialize<CalcNode[]>(fs)!;
                    if (!Nodes.Any(n => n.SearchAttribute == name))
                    {
                        throw new Exception("Указанный узел расчёта отсутствует в конфигурационном файле");
                    }
                    node = Nodes.Where(n => n.SearchAttribute == name).FirstOrDefault()!;
                }
                else
                {
                    Logger.LogError($"В файле Nodes.json XML некорректного формата");
                }
            }
            CalcService calcService = await CalcServiceCollector.GetRecalcCalcService(node);
            return calcService;
        }
    }
}
