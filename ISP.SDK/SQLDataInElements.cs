using System.Runtime.InteropServices;
using System.Data.SqlClient;
using ISP.SDK.IspObjects;
using Attribute = ISP.SDK.IspObjects.Attribute;

namespace ISP.SDK
{
    [ComVisible(false)]
    public class SQLDataInElements
#nullable disable
    {
        private string elementRootPath = "";
        public Element GetElement(Guid elementId, string connectionString)
        {
            Element element = MapSqlDataInElement(GetSqlExpression(elementId), connectionString);
            return element;
        }
        public Elements GetElements(string modelName, string elementPath, string elementTemplateName, string attributeName, bool hierarchy, string connectionString)
        {
            Elements elements = MapSqlDataInElements(GetSqlExpression(modelName, elementPath, elementTemplateName, attributeName), connectionString);
            if (hierarchy)
            {
                Elements tree = new Elements();
                tree.AddRange(elements.Where(item => item.Path == elementRootPath).Select(item => BuildModelTree(item, elements)));
                return tree;
            }
            return elements;
        }
        private Elements MapSqlDataInElements(string sqlExpression, string connectionString)
        {
            /*
             SQL Query In GetSqlExpression
            */
            Elements elements = new Elements();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(sqlExpression, connection);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string guid = dr["ElementId"].ToString();
                    if (elementRootPath.Length == 0) elementRootPath = dr["ElementPath"].ToString(); //Order by Path!
                    if (!elements.Any(item => item.Id == guid))
                    {
                        elements.Add(new Element
                        {
                            Id = guid,
                            Name = dr["ElementName"].ToString(),
                            Description = dr["ElementDescription"].ToString(),
                            ParentId = dr["ElementParentId"].ToString(),
                            Creation = (DateTime)dr["ElementCreationDate"],
                            Template = dr["ElementTemplateName"].ToString(),
                            Path = dr["ElementPath"].ToString()
                        });
                    }
                    if (dr["AttributeId"] != DBNull.Value)
                    {
                        elements.FirstOrDefault(item => item.Id == guid).Attributes.Add(new Attribute
                        {
                            Id = dr["AttributeId"].ToString(),
                            ParentId = dr["AttributeParentId"].ToString(),
                            Name = dr["AttributeName"].ToString(),
                            Path = dr["AttributePath"].ToString(),
                            DataReference = dr["AttributeDataReferenceProperties"].ToString(),
                            ValueType = dr["AttributeDataReferenceType"].ToString(),
                            Value = dr["AttributeValue"].ToString()
                        });
                    }
                }
                dr.Close();
            }

            return elements;
        }
        private Element MapSqlDataInElement(string sqlExpression, string connectionString)
        {
            Element element = new Element();
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand(sqlExpression, connection);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    string guid = dr["ElementId"].ToString();
                    element.Id = guid;
                    element.Name = dr["ElementName"].ToString();
                    element.Description = dr["ElementDescription"].ToString();
                    element.ParentId = dr["ElementParentId"].ToString();
                    element.Creation = (DateTime)dr["ElementCreationDate"];
                    element.Template = dr["ElementTemplateName"].ToString();
                    element.Path = dr["ElementPath"].ToString();
                    if (dr["AttributeId"] != DBNull.Value)
                    {
                        element.Attributes.Add(new Attribute
                        {
                            Id = dr["AttributeId"].ToString(),
                            ParentId = dr["AttributeParentId"].ToString(),
                            Name = dr["AttributeName"].ToString(),
                            Path = dr["AttributePath"].ToString(),
                            DataReference = dr["AttributeDataReferenceProperties"].ToString(),
                            ValueType = dr["AttributeDataReferenceType"].ToString(),
                            Value = dr["AttributeValue"].ToString()
                        });
                    }
                }
                dr.Close();
            }
            return element;
        }
        private Element BuildModelTree(Element element, Elements elements)
        {
            element.Children.AddRange(elements.Where(item => item.ParentId == element.Id).Select(item => BuildModelTree(item, elements)));
            return element;
        }
        private string GetSqlExpression(string modelName, string elementPath, string elementTemplateName, string attributeName)
        {
            string whereExpression = attributeName == "*" ? "" : $@"WHERE ER.ElementId IN (SELECT DISTINCT EATT.ElementId FROM dbo.ElementAttribute EATT WHERE EATT.Name = '{attributeName.Replace("*", "%")}')";
            return $@"SELECT M.Name As ModelName
                   ,M.Description As ModelDescription
                    ,M.CreationDate As ModelCreationDate
                    ,ER.ElementId
                    ,E.Name As ElementName
                    ,E.Description As ElementDescription
                    ,ER.ParentId As ElementParentId
                    ,ER.CreationDate As ElementCreationDate
                    ,ET.Name As ElementTemplateName
                    ,EP.Path As ElementPath
					,EA.ObjectId As AttributeId
					,EA.ParentId As AttributeParentId
                    ,EA.Name As AttributeName
                    ,EA.AttributePath
                    ,EA.DataReferenceProperties As AttributeDataReferenceProperties
                    ,DR.Name As AttributeDataReferenceType
                    ,(SELECT TOP 1 TV.Value FROM dbo.TimedValue TV WHERE TV.ElementAttributeId = EA.ObjectId ORDER By TV.Time DESC) As AttributeValue
                    FROM [ISP].[dbo].[ElementRef] ER
                    INNER JOIN dbo.Model M ON ER.ModelId = M.ObjectId AND M.Name like '{modelName.Replace("*", "%")}'
                    INNER JOIN dbo.ElementPath EP ON EP.ElementId = ER.ElementId AND EP.Path like '{elementPath.Replace("*", "%")}'
                    INNER JOIN dbo.Element E ON EP.ElementId = E.ObjectId AND E.IsDeleted = 0
                    INNER JOIN dbo.ElementTemplate ET ON ET.ObjectId = E.ElementTemplateId AND ET.IsDeleted = 0 AND ET.Name like '{elementTemplateName.Replace("*", "%")}'
                    LEFT JOIN dbo.ElementAttribute EA ON EA.ElementId = ER.ElementId
                    LEFT JOIN dbo.DataReference DR ON DR.ObjectId = EA.DataReferenceId AND DR.IsDeleted = 0
                    {whereExpression}
                    ORDER BY EP.Path, EA.AttributePath";
        }
        private string GetSqlExpression(Guid elementId)
        {
            return $@"SELECT M.Name As ModelName
                   ,M.Description As ModelDescription
                    ,M.CreationDate As ModelCreationDate
                    ,ER.ElementId
                    ,E.Name As ElementName
                    ,E.Description As ElementDescription
                    ,ER.ParentId As ElementParentId
                    ,ER.CreationDate As ElementCreationDate
                    ,ET.Name As ElementTemplateName
                    ,EP.Path As ElementPath
					,EA.ObjectId As AttributeId
					,EA.ParentId As AttributeParentId
                    ,EA.Name As AttributeName
                    ,EA.AttributePath
                    ,EA.DataReferenceProperties As AttributeDataReferenceProperties
                    ,DR.Name As AttributeDataReferenceType
                    ,(SELECT TOP 1 TV.Value FROM dbo.TimedValue TV WHERE TV.ElementAttributeId = EA.ObjectId ORDER By TV.Time DESC) As AttributeValue
                    FROM [ISP].[dbo].[ElementRef] ER
                    INNER JOIN dbo.Model M ON ER.ModelId = M.ObjectId
                    INNER JOIN dbo.ElementPath EP ON EP.ElementId = ER.ElementId
                    INNER JOIN dbo.Element E ON EP.ElementId = E.ObjectId AND E.IsDeleted = 0 AND E.ObjectId = '{elementId}'
                    INNER JOIN dbo.ElementTemplate ET ON ET.ObjectId = E.ElementTemplateId AND ET.IsDeleted = 0
                    LEFT JOIN dbo.ElementAttribute EA ON EA.ElementId = ER.ElementId
                    LEFT JOIN dbo.DataReference DR ON DR.ObjectId = EA.DataReferenceId AND DR.IsDeleted = 0
                    ORDER BY EP.Path, EA.AttributePath";
        }
    }
}
