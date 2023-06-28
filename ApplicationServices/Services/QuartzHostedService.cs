using System.Collections.Concurrent;
using System.Text.Json;
using System.Xml.Linq;
using ApplicationServices.Calculator;
using ApplicationServices.Scheduller.Jobs;
using ApplicationServices.Scheduller.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Spi;
using TSDBWorkerAPI.Models;

namespace ApplicationServices.Services;

public class QuartzHostedService : IHostedService
#nullable disable
{
    ILogger<CalcService> Logger { get; set; }
    CalcServiceCollector CalcServiceCollector { get; set; }
    private readonly ISchedulerFactory schedulerFactory;
    private readonly IJobFactory jobFactory;

    public QuartzHostedService(
        ISchedulerFactory schedulerFactory,
        IJobFactory jobFactory,
        ILogger<CalcService> logger,
        CalcServiceCollector calcServiceCollector)
    {
        this.schedulerFactory = schedulerFactory;
        this.jobFactory = jobFactory;
        Logger = logger;
        CalcServiceCollector = calcServiceCollector;
    }
    public IScheduler Scheduler { get; set; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        Scheduler.JobFactory = jobFactory;
        CalcNode[] nodes = await ReadNodes();
        foreach (var node in nodes)
        {
            JobForSchedule<SchedulledCalcsJob> schedullerJob = new(node, "SchedulleNodes");
            try
            {
                await Scheduler.ScheduleJob(schedullerJob.JobDetail, schedullerJob.Trigger, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка запуска узла {node.SearchAttribute} с режимом запуска по расписанию: {ex.Message}");
            }
            JobForTrigger<TriggerCalcsJob> triggerJob = new(node, "TriggerNodes");
            try
            {
                await Scheduler.ScheduleJob(triggerJob.JobDetail, triggerJob.Trigger, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Ошибка запуска узла {node.SearchAttribute} с режимом запуска по триггеру: {ex.Message}");
            }
        }
        await Scheduler.Start(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Scheduler?.Shutdown(cancellationToken)!;
    }
    private async Task<CalcNode[]> ReadNodes()
    {
        CalcNode[] nodes = new CalcNode[] { };
        using (FileStream fs = new("Nodes.json", FileMode.OpenOrCreate))
        {
            nodes = JsonSerializer.Deserialize<CalcNode[]>(fs)!;
        }
        ConcurrentDictionary<string, CalcNode> CalcNodes = new();
        
        foreach (var node in nodes)
        {
            CalcNodes.TryAdd(node.SearchAttribute, node);
            await CalcServiceCollector.AddSchedulledCalcService(node);
        }
        return nodes.ToArray();
    }
}