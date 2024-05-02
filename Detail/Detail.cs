using Smartproj.Utils;
using System.Xml.Serialization;

namespace Smartproj
{
    public abstract class Detail : Tree
    {
        public abstract DetailLayoutTypeEnum LayoutType { get; set; }
        public abstract DetailTypeEnum DetailType { get; }
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
