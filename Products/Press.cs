using Smartproj.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    public class PressCollection : IEnumerable<Press>
    {
        private List<Press> mPress;
        public Press this[string _code] => mPress.SingleOrDefault(x => x.Code == _code);
        public Press this[int _id] => mPress.SingleOrDefault(x => x.Id == _id);
        public void Add(Press _press)
        {
            mPress.Add(_press);
        }
        public IEnumerator<Press> GetEnumerator()
        {
            return mPress.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mPress.GetEnumerator();
        }
        public PressCollection()
        {
            mPress = new List<Press>();
        }
    }
    public class Press
    {
        [XmlElement]
        public string Code { get; set; }
        [XmlElement]
        public int Id { get; set; }
        [XmlElement]
        public int OpId { get; set; }
        [XmlElement]
        public SizeF PrintArea { get; set; }
        [XmlElement]
        public Margins PrintMargins { get; set; }
        [XmlElement]
        public bool IsRoll { get; set; }
        public Press() { }
    }
}
