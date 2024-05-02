using Smartproj.Utils;
using System.Xml.Serialization;

namespace Smartproj
{
    public class ClassicBook : Product
    {
        private string mProductCode;
        [XmlElement]
        public override string ProductCode
        {
            get => mProductCode;
            set
            {
                if (value != null && value != "")
                {
                    if (Name == null || Name == "")
                    {
                        Name = GetDefaultName(value);
                    }
                    mProductCode = value;
                }
            }
        }
        public ClassicBook(string _productCode, string _name) : base()
        {
            Binding = BindingEnum.Glue;
            Name = _name;
            ProductCode = _productCode;
        }
        protected ClassicBook() : base()
        {
        }
    }
}
