using System;
using System.Xml.Serialization;

namespace Smartproj
{
    public abstract class AbstractOutputProvider : AbstractController, IOutputProvider
    {
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        [XmlElement]
        public string Destination { get; set; }
        public override void Start(object[] _settings)
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
