using Smartproj.Utils;
using System.Drawing;
using System.Xml.Serialization;

namespace Smartproj
{
    public abstract class Detail : Tree
    {
        public abstract DetailLayoutTypeEnum LayoutType { get; set; }
        public abstract DetailTypeEnum DetailType { get; }
        public MaterialsCollection Materials => ((Product)Parent).Materials[KeyId];
        public Size Size { get; set; }
        public Press Press { get; set; }    
        public bool DuplexLayout { get; set; }
        public Coating Coating 
        {
            get { return Materials?.Coating; } 
            set
            {
                ((Product)Parent).Materials.Add(value);
            }
        }
        public Paper Paper
        {
            get { return Materials?.Paper; }
            set
            {
                ((Product)Parent).Materials.Add(value);
            }
        }
        protected Detail(): base() { }
    }
    public class BlockDetail : Detail
    {
        [XmlElement]
        public override DetailLayoutTypeEnum LayoutType { get; set; }
        [XmlElement]
        public override DetailTypeEnum DetailType { get; } = DetailTypeEnum.Block;
        public BlockDetail() : base()
        {
            KeyId = "BLK";
        }
    }
    public class CoverDetail : Detail
    {
        [XmlElement]
        public override DetailLayoutTypeEnum LayoutType { get; set; }
        [XmlElement]
        public override DetailTypeEnum DetailType { get; } = DetailTypeEnum.Cover;
        public CoverDetail() : base()
        {
            KeyId = "CVR";
        }
    }

}
