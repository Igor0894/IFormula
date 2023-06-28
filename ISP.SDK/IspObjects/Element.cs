using System.Runtime.InteropServices;

namespace ISP.SDK.IspObjects
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Element
#nullable disable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParentId { get; set; }
        public DateTime Creation { get; set; }
        public string Template { get; set; }
        public string Path { get; set; }
        public Attributes Attributes { get; set; } = new Attributes();
        public Elements Children { get; set; } = new Elements();
    }
}
