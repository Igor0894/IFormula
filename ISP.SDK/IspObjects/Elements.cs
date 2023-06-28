using System.Collections;
using System.Runtime.InteropServices;

namespace ISP.SDK.IspObjects
{
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    public class Elements : IEnumerable<Element>
#nullable disable
    {
        private readonly List<Element> _elements = new List<Element>();
        public int Count => _elements.Count;
        public Element[] ToArray() => _elements.ToArray();
        public Elements GetElementsWithAttributes()
        {
            Elements elements = new Elements();
            elements.AddRange(_elements.FindAll(item => item.Attributes.Count > 0));
            return elements;
        }

        public void Add(Element element) => _elements.Add(element);
        public Element Item(int index)
        {
            if (index > -1 && index < Count) return _elements[index];
            return null;
        }
        public Element Item(string name)
        {
            return _elements.Find(e => e.Name.ToLower() == name.ToLower());
        }
        [ComVisible(false)]
        internal void AddRange(IEnumerable<Element> elements) => _elements.AddRange(elements);
        [ComVisible(false)]
        public IEnumerator<Element> GetEnumerator() => _elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_elements).GetEnumerator();
    }
}
