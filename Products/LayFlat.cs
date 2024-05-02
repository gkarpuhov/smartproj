using Smartproj.Utils;
using Smartproj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace smartproj.Products
{
    public class LayFlat : Product
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
        public LayFlat(string _productCode, string _name) : base()
        {
            Binding = BindingEnum.LayFlat;
            Name = _name;
            ProductCode = _productCode;
        }
        protected LayFlat() : base()
        {
        }
    }
}
