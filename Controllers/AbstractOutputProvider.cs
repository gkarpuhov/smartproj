using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj
{
    public class AbstractOutputProvider : AbstractController, IOutputProvider
    {
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        public string Destination { get; set; }
        public override bool Start(params object[] _settings)
        {
            throw new NotImplementedException();
        }
        protected override void Dispose(bool _disposing)
        {
            throw new NotImplementedException();
        }
    }
}
