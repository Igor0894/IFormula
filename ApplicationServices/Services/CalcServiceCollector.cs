using ApplicationServices.Calculator;
using ApplicationServices.Scheduller.Models;
using Interpreter.Delegates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using TSDBWorkerAPI;

namespace ApplicationServices.Services
{
    public class CalcServiceCollector
#nullable disable
    {
        private ILogger<CalcNodeService> CalcServiceLogger { get; set; }
        private ILogger<SchedulledCalcsHandler> CalcHandlerLogger { get; set; }
        private string ConnectionString { get; set; }
        private TsdbClient TsdbWorker { get; set; }
        private ConcurrentDictionary<string, CalcNodeService> CalcServices { get; set; } = new ConcurrentDictionary<string, CalcNodeService>();
        public CalcServiceCollector(ILogger<CalcNodeService> calcServiceLogger, ILogger<SchedulledCalcsHandler> calcHandlerLogger, TsdbClient tsdbWorker, IConfiguration configuration
            , ILogger<TsdbClient> tsdbClientLogger)
        {
            CalcServiceLogger = calcServiceLogger;
            CalcHandlerLogger = calcHandlerLogger;
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
            TsdbWorker = tsdbWorker;
            TsdbWorker.logger = tsdbClientLogger;
            TSDB.TsdbClient = tsdbWorker;
        }
        public string[] GetNodesNames()
        {
            return CalcServices.Keys.ToArray();
        }
        public async Task AddCalcService(CalcNode node)
        {
            CalcNodeService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node);
            await calcService.InitializeModel(CalcMode.Schedulled);
            if (CalcServices.ContainsKey(node.SearchAttribute)) { CalcServices[node.SearchAttribute] = calcService; }
            else { CalcServices.TryAdd(node.SearchAttribute, calcService); }
        }
        public void DeleteCalcService(string name)
        {
            if (CalcServices.ContainsKey(name)) { CalcServices.TryRemove(name, out CalcNodeService calcService); }
        }
        public CalcNodeService GetSchedulledAndTriggeredCalcService(string name)
        {
            return CalcServices[name];
        }
        public async Task<CalcNodeService> GetRecalcCalcService(CalcNode node)
        {
            CalcNodeService calcService = new(CalcServiceLogger, CalcHandlerLogger, ConnectionString, node);
            await calcService.InitializeModel(CalcMode.Recalc);
            return calcService;
        }
        public CalcNodeService GetCalcServiceByElementName(string elementName)
        {
            foreach(var calcService in CalcServices.Values)
            {
                if(calcService.ExistCalcElementByName(elementName)) return calcService;
            }
            throw new Exception($"Запрашиваемый элемент [{elementName}] не найден в запущенных узлах расчётов.");
        }
    }
}
