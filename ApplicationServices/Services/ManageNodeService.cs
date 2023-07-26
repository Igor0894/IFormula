using ApplicationServices.Scheduller.Jobs;
using ApplicationServices.Scheduller.Models;
using Quartz.Impl;
using Quartz;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Quartz.Impl.Matchers;
using Quartz.Spi;
using Newtonsoft.Json;
using System.Text;

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
        public async Task<IResult> StopAndDeleteNode(string name)
        {
            DeleteNodeFromFile(name);
            if (await TryDeleteJobs(name))
            {
                Logger.LogInformation($"Остановлен и удалён узел расчёта: {name}");
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
            CalcService calcService = await GetCalcServiceForRecalc(name);
            calcService.RecalcNode(start, end);
            recalcSemaphore.Release();
        }
        private async Task StartJob(string name)
        {
            CalcNode node = await ReadNodeAndAddCalcService(name);
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
        private async Task<CalcNode> ReadNodeAndAddCalcService(string name)
        {
            CalcNode[] Nodes = GetNodesFromFile();
            CalcNode node = default!;
            if (!Nodes.Any(n => n.SearchAttribute == name))
            {
                throw new Exception("Указанный узел расчёта отсутствует в конфигурационном файле");
            }
            node = Nodes.Where(n => n.SearchAttribute == name).FirstOrDefault()!;
            await CalcServiceCollector.AddCalcService(node);
            return node;
        }
        private CalcNode[] GetNodesFromFile()
        {
            CalcNode[] Nodes = new CalcNode[] { };
            using (FileStream fs = new("Nodes.json", FileMode.OpenOrCreate))
            {
                if (fs != null)
                {
                    Nodes = System.Text.Json.JsonSerializer.Deserialize<CalcNode[]>(fs)!;
                }
                else
                {
                    Logger.LogError($"В файле Nodes.json XML некорректного формата");
                }
            }
            return Nodes;
        }
        private void DeleteNodeFromFile(string name)
        {
            CalcNode[] Nodes = GetNodesFromFile();
            if (!Nodes.Any(n => n.SearchAttribute == name)) { return; }
            Nodes = Nodes.Where(n => n.SearchAttribute != name).ToArray();
            string json = JsonConvert.SerializeObject(Nodes, Formatting.Indented);
            File.WriteAllText("Nodes.json", json, Encoding.UTF8);
        }
        private async Task<CalcService> GetCalcServiceForRecalc(string name)
        {
            CalcNode[] Nodes = GetNodesFromFile();
            CalcNode node = default!;
            if (!Nodes.Any(n => n.SearchAttribute == name))
            {
                throw new Exception("Указанный узел расчёта отсутствует в конфигурационном файле");
            }
            node = Nodes.Where(n => n.SearchAttribute == name).FirstOrDefault()!;
            CalcService calcService = await CalcServiceCollector.GetRecalcCalcService(node);
            return calcService;
        }
    }
}
