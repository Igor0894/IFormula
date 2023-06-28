using System.Collections;
using System.Runtime.InteropServices;

namespace ISP.SDK.IspObjects
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Attributes : IEnumerable<Attribute>
#nullable disable
    {
        private readonly List<Attribute> _attributes = new List<Attribute>();
        public int Count => _attributes.Count;
        public Attribute[] ToArray() => _attributes.ToArray();
        public Attributes Properties
        {
            get
            {
                Attributes attributes = new Attributes();
                attributes.AddRange(_attributes.FindAll(e => e.ValueType != AttributeValueType.TSDB && e.ValueType != AttributeValueType.PI));
                return attributes;
            }
        }
        public Attributes Points
        {
            get
            {
                Attributes attributes = new Attributes();
                attributes.AddRange(_attributes.FindAll(e => e.ValueType == AttributeValueType.TSDB || e.ValueType == AttributeValueType.PI));
                return attributes;
            }
        }
        public void Add(Attribute attribute) => _attributes.Add(attribute);
        public Attribute Item(int index) => index > -1 && index < Count ? _attributes[index] : null;
        public Attribute Item(string name) => _attributes.Find(e => e.Name.ToLower() == name.ToLower());

        public Attributes Children(string attributeId)
        {
            Attributes attributes = new Attributes();
            attributes.AddRange(_attributes.FindAll(e => e.ParentId == attributeId));
            return attributes;
        }
        public bool Contains(string name)
        {
            return _attributes.FindIndex((Attribute e) => e.Name.ToLower() == name.ToLower()) != -1;
        }
        [ComVisible(false)]
        public void AddRange(IEnumerable<Attribute> attributes) => _attributes.AddRange(attributes);
        [ComVisible(false)]
        public IEnumerator<Attribute> GetEnumerator() => _attributes.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_attributes).GetEnumerator();
    }
}
