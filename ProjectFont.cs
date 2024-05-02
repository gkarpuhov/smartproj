using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Smartproj
{
    public class ProjectFont
    {
        [XmlElement]
        public string Name { get; set; }
        [XmlElement]
        public bool IsSystem  { get; }
        [XmlElement]
        public string Location { get; set; }
        [XmlElement]
        public int FontId => Name.StringHashCode40() ^ Location.StringHashCode40();
        [XmlElement]
        protected string Bin { get; set; }
        public static IEnumerable<ProjectFont> GetSystemFonts()
        {
            List<ProjectFont> fonts = new List<ProjectFont>();  
            return fonts;
        }
        public ProjectFont(string _name, bool _isSystem, string _location)
        {
            Name = _name;
            IsSystem = _isSystem;
            Location = _location;
            InstalledFontCollection installedFontCollection = new InstalledFontCollection();

            for (int i = 0; i < installedFontCollection.Families.Length; i++)
            {

            }
        }   
    }
}
