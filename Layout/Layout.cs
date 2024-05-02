using Smartproj.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    public class Layout
    {
        public Logger Log => Owner?.Log;
        public LayoutCollection Owner { get; set; }
        [XmlElement]
        public Size ProductSize { get; set; }
        public TemplateCollection TemplateCollection { get; set; }
        public Layout()
        {
        }
    }
    public class LayoutCollection : IEnumerable<Layout>
    {
        private List<Layout> mItems;
        public Product Owner { get; }
        public int Count => mItems.Count;
        public Layout this[int _index] => mItems[_index];
        public Layout this[Size _trim] => mItems.SingleOrDefault(x => (x.ProductSize.Width == _trim.Width && x.ProductSize.Height == _trim.Height));
        public Logger Log => Owner?.Log;
        public LayoutCollection(Product _owner)
        {
            Owner = _owner;
            mItems = new List<Layout>();
        }
        public void Add(Layout _item)
        {
            if (_item != null)
            {
                _item.Owner = this;
                mItems.Add(_item);
            }
        }
        public void Clear()
        {
            foreach (Layout item in mItems)
            {
                item.Owner = null;
            }
            mItems.Clear();
        }
        public IEnumerator<Layout> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }

}
