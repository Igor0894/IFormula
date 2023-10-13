using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ApplicationServices.Services;

namespace IFormula.Controllers
{
    [Route("NodesManager")]
    [ApiController]
    [EnableCors]
    public class NodesManagerController : Controller
    {
        NodesManagerService ManageNodeService { get; set; }
        public NodesManagerController(NodesManagerService ManageNodeService)
        {
            this.ManageNodeService = ManageNodeService;

        }
        [HttpPost("AddOrRestartNode")]
        public async Task<IResult> AddOrRestartNode(string name)
        {
            return await ManageNodeService.AddOrStartNode(name);
        }
        [HttpPost("StopNode")]
        public async Task<IResult> StopNode(string name)
        {
            return await ManageNodeService.StopNode(name);
        }
        [HttpPost("StopAndDeleteNode")]
        public async Task<IResult> StopAndDeleteNode(string name)
        {
            return await ManageNodeService.StopAndDeleteNode(name);
        }
        [HttpPost("RecalcNode")]
        public async Task<IResult> RecalcNode(string name, string startTimeLocal, string endTimeLocal)
        {
            return await ManageNodeService.RecalcNode(name, startTimeLocal, endTimeLocal);
        }
        [HttpGet("GetElementCalcAtributesCurrentValue")]
        public async Task<Dictionary<string,string>> GetElementCalcAtributesCurrentValue(string elementName)
        {
            Task<Dictionary<string, string>> task = new Task<Dictionary<string, string>>(() => ManageNodeService.GetElementCalcAtributesValue(elementName));
            task.Start();
            await task.WaitAsync(new TimeSpan(0, 5, 0));
            return task.Result;
        }
    }
}
