using System.Runtime.InteropServices;
using ISP.SDK.IspObjects;

namespace ISP.SDK
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Server
#nullable disable
    {
        public string Address { get; set; } = "local";
        public string Database { get; set; } = "ISP";
        public bool SSPI { get; set; } = true;
        public string User { get; set; } = "sa";
        public string Password { get; set; } = "";
        public string ConnectionString
        {
            get; set;
            //get => SSPI ? $"Data Source={Address};Initial Catalog={Database};Integrated Security=True;" : $"Data Source={Address};Initial Catalog={Database};User Id={User};Password={Password}";
        }
        public Elements GetElements(string modelName = "*", string elementPath = "*", string elementTemplateName = "*", string attributeName = "*", bool hierarchy = false)
        {
            SQLDataInElements data = new SQLDataInElements();
            Elements elements = data.GetElements(modelName, elementPath, elementTemplateName, attributeName, hierarchy, ConnectionString);
            return elements;
        }
    }
}
