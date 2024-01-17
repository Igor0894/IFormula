using System.Runtime.InteropServices;
using System.Xml;

namespace ISP.SDK.IspObjects
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Attribute
#nullable disable
    {
        private XmlElement _dataReferenceXml;
        private string _sqlvalue;
        public string Id { get; set; }
        public string ParentId { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
        public string DataReference
        {
            get => _dataReferenceXml.OuterXml;
            set { _dataReferenceXml = GetXmlElement(value); }
        }
        public string Value
        {
            get => GetValue();
            set { _sqlvalue = value; }
        }
        public AttributeValueType ValueType { get; set; }
        public Attributes Children { get; set; } = new Attributes();
        private XmlElement GetXmlElement(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            return doc.DocumentElement;
        }
        private string GetValue()
        {
            if (ValueType == AttributeValueType.SQL) return _sqlvalue;
            if (ValueType == AttributeValueType.Static) return _dataReferenceXml.GetAttribute("Value");
            if (ValueType == AttributeValueType.TSDB || ValueType == AttributeValueType.PI) return _dataReferenceXml.GetAttribute("Tag");
            return $"Для типа {ValueType} нет определения";
        }
    }
}
