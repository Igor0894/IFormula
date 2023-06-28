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
        [HttpPost("AddOrRestart")]
        public async Task<IResult> AddOrRestart(string name)
        {
            return await ManageNodeService.AddOrStartNode(name);
        }
        [HttpPost("RecalcNode")]
        public async Task<IResult> RecalcNode(string name, string startTimeLocal, string endTimeLocal)
        {
            return await ManageNodeService.RecalcNode(name, startTimeLocal, endTimeLocal);
        }
    }
}
