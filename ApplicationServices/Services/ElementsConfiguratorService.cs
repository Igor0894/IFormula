using ApplicationServices.Calculator;
using ApplicationServices.Entityes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using ApplicationServices.Models;
using ApplicationServices.Scheduller.Models;

namespace ApplicationServices.Services
{
    public class ElementsConfiguratorService
#nullable disable
    {
        private ILogger<ElementsConfiguratorService> ElementsConfiguratorLogger { get; set; }
        private string ConnectionString { get; set; }
        private ILogger<CalcNodeService> CalcServiceLogger { get; set; }
        private ILogger<SchedulledCalcsHandler> CalcHandlerLogger { get; set; }
        public ElementsConfiguratorService(ILogger<ElementsConfiguratorService> formulaConfiguratorLogger, IConfiguration configuration
            , ILogger<CalcNodeService> calcServiceLogger, ILogger<SchedulledCalcsHandler> calcHandlerLogger)
        {
            ElementsConfiguratorLogger = formulaConfiguratorLogger;
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
            CalcServiceLogger = calcServiceLogger;
            CalcHandlerLogger = calcHandlerLogger;
        }
        public async Task<Dictionary<string, string>> GetTestElementCalcAtributesValue(Guid elementId)
        {
            CalcsHandler calcsHandler = new CalcsHandler(CalcHandlerLogger, CalcServiceLogger);
            CalcNode[] nodes = GetNodesFromFile();
            await calcsHandler.TestElementInitialization(ConnectionString, elementId, nodes);
            calcsHandler.CalculateTestElements(DateTime.Now);

            Dictionary<string, string> atributesValue = new Dictionary<string, string>();
            foreach (var element in calcsHandler.CalcElements)
            {
                if (element.Id == elementId.ToString())
                {
                    CalcElement calcElement = element;
                    foreach (var atribute in calcElement.CalcAttributes)
                    {
                        atributesValue.Add(atribute.Name, atribute.Value.ToString());
                    }
                    break;
                }
            }
            return atributesValue;
        }
        private CalcNode[] GetNodesFromFile()
        {
            CalcNode[] nodes = new CalcNode[] { };
            using (FileStream fs = new("Nodes.json", FileMode.OpenOrCreate))
            {
                if (fs != null)
                {
                    nodes = System.Text.Json.JsonSerializer.Deserialize<CalcNode[]>(fs)!;
                }
                else
                {
                    throw new Exception($"В файле Nodes.json XML некорректного формата");
                }
            }
            return nodes;
        }
        public TreeViewNode[] GetChildren(string id, bool isRoot)
        {
            if (isRoot)
            {
                return GetBaseElementTreeViewNodes();
            }
            else
            {
                return LoadChildElementsForElement(id);
            }
        }
        private TreeViewNode[] GetBaseElementTreeViewNodes()
        {
            IspModel[] ispModels = LoadModels();
            List<TreeViewNode> nodes = new List<TreeViewNode>();
            foreach (IspModel ispModel in ispModels)
            {
                TreeViewNode node = new TreeViewNode()
                {
                    id = ispModel.Guid.ToString(),
                    text = ispModel.Name,
                    parent = "#"
                };
                nodes.Add(node);
            }
            foreach (IspModel model in ispModels)
            {
                foreach (IspElement ispElement in LoadChildElementsForModel(model.Guid))
                {
                    TreeViewNode node = new TreeViewNode()
                    {
                        id = ispElement.Guid.ToString(),
                        text = ispElement.Name,
                        parent = model.Guid.ToString()
                    };
                    nodes.Add(node);
                }
            }
            return nodes.ToArray();
        }
        private IspModel[] LoadModels()
        {
            List<IspModel> models = new List<IspModel>();
            string query = "SELECT [ObjectId], [Name] FROM Model";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    IspModel ispModel = new IspModel()
                    {
                        Guid = Guid.Parse(dr["ObjectId"].ToString()),
                        Name = dr["Name"].ToString()
                    };
                    models.Add(ispModel);
                }
                dr.Close();
            }
            return models.ToArray();
        }
        private IspElement[] LoadChildElementsForModel(Guid modelGuid)
        {
            List<IspElement> elements = new List<IspElement>();
            string query = "SELECT er.[ElementId], e.[Name]" +
                "\r\nFROM ElementRef er INNER JOIN Element e ON er.ElementId=e.ObjectId\r\n" +
                $"WHERE er.ModelId='{modelGuid}' AND er.ParentId IS NULL";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    IspElement ispElement = new IspElement()
                    {
                        Guid = Guid.Parse(dr["ElementId"].ToString()),
                        Name = dr["Name"].ToString()
                    };
                    elements.Add(ispElement);
                }
                dr.Close();
            }
            return elements.ToArray();
        }
        private TreeViewNode[] LoadChildElementsForElement(string parentElementGuid)
        {
            List<IspElement> elements = new List<IspElement>();
            string query = "SELECT er.[ElementId], e.[Name]\r\n" +
                "FROM ElementRef er INNER JOIN Element e ON er.ElementId=e.ObjectId\r\n" +
                $"WHERE er.ParentId='{parentElementGuid}'";
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(query, connection);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    IspElement ispElement = new IspElement()
                    {
                        Guid = Guid.Parse(dr["ElementId"].ToString()),
                        Name = dr["Name"].ToString()
                    };
                    elements.Add(ispElement);
                }
                dr.Close();
            }

            List<TreeViewNode> nodes = new List<TreeViewNode>();
            foreach (IspElement ispElement in elements)
            {
                TreeViewNode node = new TreeViewNode()
                {
                    id = ispElement.Guid.ToString(),
                    text = ispElement.Name,
                    parent = parentElementGuid
                };
                nodes.Add(node);
            }

            return nodes.ToArray();
        }
    }
}
