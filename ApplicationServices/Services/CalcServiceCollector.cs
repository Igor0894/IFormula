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
        private ConcurrentDictionary<string, CalcService> CalcServices { get; set; } = new ConcurrentDictionary<string, CalcService>();
        public async Task AddCalcService(CalcNode node)
        {
            CalcService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node, TsdbWorker);
            await calcService.InitializeModel(CalcMode.Schedulled);
            if (CalcServices.ContainsKey(node.SearchAttribute)) { CalcServices[node.SearchAttribute] = calcService; }
            else { CalcServices.TryAdd(node.SearchAttribute, calcService); }
        }
        public void DeleteCalcService(string name)
        {
            if (CalcServices.ContainsKey(name)) { CalcServices.TryRemove(name, out CalcService calcService); }
        }
        public CalcService GetSchedulledAndTriggeredCalcService(string name)
        {
            return CalcServices[name];
        }
        public async Task<CalcService> GetRecalcCalcService(CalcNode node)
        {
            CalcService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node, TsdbWorker);
            await calcService.InitializeModel(CalcMode.Recalc);
            return calcService;
        }
    }
}
