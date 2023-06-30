using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ApplicationServices.Services;

namespace IFormula.Controllers
{
    [Route("")]
    [ApiController]
    [EnableCors]
    public class FormulaController : Controller
    {
        ManageNodeService ManageNodeService { get; set; }
        public FormulaController(ManageNodeService ManageNodeService)
        {
            this.ManageNodeService = ManageNodeService;

        }
        [HttpPost("AddOrRestartNode")]
        public async Task<IResult> AddOrRestartNode(string name)
        {
            return await ManageNodeService.AddOrStartNode(name);
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
    }
}
