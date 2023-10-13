using ApplicationServices.Models;
using ApplicationServices.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace IFormula.Controllers
{
    [Route("ElementsConfigurator")]
    [ApiController]
    [EnableCors]
    public class ElementsConfiguratorController : Controller
    {
        ElementsConfiguratorService FormulaConfiguratorService { get; set; }
        public ElementsConfiguratorController(ElementsConfiguratorService formulaConfiguratorService)
        {
            FormulaConfiguratorService = formulaConfiguratorService;
        }
        [HttpGet("GetTestElementCalcAtributesValue")]
        public async Task<Dictionary<string, string>> GetTestElementCalcAtributesValue(Guid elementId)
        {
            var result = await FormulaConfiguratorService.GetTestElementCalcAtributesValue(elementId);
            return result;
        }
        [HttpGet("GetChildren")]
        public JsonResult GetChildren(string id, bool isRoot)
        {
            TreeViewNode[] items = FormulaConfiguratorService.GetChildren(id, isRoot);
            return new JsonResult(items);
        }
    }
}
