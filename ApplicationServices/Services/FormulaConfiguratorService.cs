using ApplicationServices.Calculator;
using ApplicationServices.Entityes;
using ISP.SDK.IspObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TSDBWorkerAPI;
using ApplicationServices.Models;
using System.Data;

namespace ApplicationServices.Services
{
    public class FormulaConfiguratorService
#nullable disable
    {
        private ILogger<FormulaConfiguratorService> FormulaConfiguratorLogger { get; set; }
        private string ConnectionString { get; set; }
        private TsdbClient TsdbWorker { get; set; }
        public FormulaConfiguratorService(ILogger<FormulaConfiguratorService> formulaConfiguratorLogger, TsdbClient tsdbWorker, IConfiguration configuration)
        {
            FormulaConfiguratorLogger = formulaConfiguratorLogger;
            ConnectionString = configuration.GetConnectionString("DefaultConnection");
            TsdbWorker = tsdbWorker;
        }
        public TreeViewNode[] GetBaseElementTreeViewNodes()
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
        private IspElement[] LoadChildElementsForElement(Guid parentElementGuid)
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
            return elements.ToArray();
        }
    }
}
