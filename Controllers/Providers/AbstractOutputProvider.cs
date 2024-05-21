using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj
{
    public abstract class AbstractOutputProvider : AbstractController, IOutputProvider
    {
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        public string Destination { get; set; }
        public override bool Start(object[] _settings)
        {
            throw new NotImplementedException();
        }
        protected override void Dispose(bool _disposing)
        {
            if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            Log?.WriteInfo("AbstractOutputProvider.Dispose", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер вывода => Ресурсы освобождены");
            if (_disposing)
            {

            }
            CurrentStatus = ProcessStatusEnum.Disposed;
        }
        protected AbstractOutputProvider() : base() 
        {
            CurrentStatus = ProcessStatusEnum.New;
        }
    }
}
