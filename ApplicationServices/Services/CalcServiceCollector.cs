using ApplicationServices.Calculator;
using ApplicationServices.Scheduller.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TSDBWorkerAPI;

namespace ApplicationServices.Services
{
    public class CalcServiceCollector
#nullable disable
    {
        private ILogger<CalcService> CalcServiceLogger { get; set; }
        private ILogger<SchedulledCalcsHandler> CalcHandlerLogger { get; set; }
        private string ConnectionString { get; set; }
        private TsdbClient TsdbWorker { get; set; }
        public CalcServiceCollector(ILogger<CalcService> calcServiceLogger, ILogger<SchedulledCalcsHandler> calcHandlerLogger, TsdbClient tsdbWorker, IConfiguration configuration
            , ILogger<TsdbClient> tsdbClientLogger)
        {
            CalcServiceLogger = calcServiceLogger;
            CalcHandlerLogger = calcHandlerLogger;
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
            TsdbWorker = tsdbWorker;
            TsdbWorker.logger = tsdbClientLogger;
        }
        private ConcurrentDictionary<string, CalcService> SchedulledCalcServices { get; set; } = new ConcurrentDictionary<string, CalcService>();
        public async Task AddSchedulledCalcService(CalcNode node)
        {
            CalcService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node, TsdbWorker);
            await calcService.InitializeModel(CalcMode.Schedulled);
            if (SchedulledCalcServices.ContainsKey(node.SearchAttribute)) { SchedulledCalcServices[node.SearchAttribute] = calcService; }
            else { SchedulledCalcServices.TryAdd(node.SearchAttribute, calcService); }
        }
        public CalcService GetSchedulledAndTriggeredCalcService(string name)
        {
            return SchedulledCalcServices[name];
        }
        public async Task<CalcService> GetRecalcCalcService(CalcNode node)
        {
            CalcService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node, TsdbWorker);
            await calcService.InitializeModel(CalcMode.Recalc);
            return calcService;
        }
    }
}
