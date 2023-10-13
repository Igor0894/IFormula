using ApplicationServices.Models;
using ApplicationServices.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace IFormula.Pages
{
    public class IspModelModel : PageModel
    {
        public string TreeViewJSON { get; set; }
        ElementsConfiguratorService FormulaConfiguratorService { get; set; }
        public IspModelModel(ElementsConfiguratorService formulaConfiguratorService)
        {
            FormulaConfiguratorService = formulaConfiguratorService;
        }
        public void OnGet()
        {
            TreeViewNode[] nodes = FormulaConfiguratorService.GetChildren(null, true);

            //Serialize to JSON string.
            this.TreeViewJSON = JsonConvert.SerializeObject(nodes);
        }
        public void OnPostSubmit(string selectedItems)
        {
            List<TreeViewNode> items = JsonConvert.DeserializeObject<List<TreeViewNode>>(selectedItems);
        }
    }
}
