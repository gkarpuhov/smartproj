using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj
{
    public class RIP
    {
        public int CriticalResolution { get; set; }
        public Product Owner { get; }
        public IQueueExtractor DataQueue { get; set; }
        public ImageProcessor ImageProcessor { get; private set; }
        public ImageProcessor CreateImageProcessor()
        {
            return ImageProcessor = new ImageProcessor(this);  
        }
        public RIP(Product _owner)
        {
            Owner = _owner;
            CriticalResolution = 200;
        }
    }
}
