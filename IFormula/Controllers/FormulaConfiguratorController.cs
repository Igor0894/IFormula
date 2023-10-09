using ApplicationServices.Models;
using ApplicationServices.Services;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Xml.Linq;

namespace IFormula.Controllers
{
    [Route("Configurator")]
    [ApiController]
    [EnableCors]
    public class FormulaConfiguratorController : Controller
    {
        FormulaConfiguratorService FormulaConfiguratorService { get; set; }
        public FormulaConfiguratorController(FormulaConfiguratorService formulaConfiguratorService)
        {
            FormulaConfiguratorService = formulaConfiguratorService;
        }
        [HttpGet("GetChildren")]
        public JsonResult GetChildren(string id, bool isRoot)
        {
            TreeViewNode[] items = FormulaConfiguratorService.GetChildren(id, isRoot);
            return new JsonResult(items);
        }
    }
}
