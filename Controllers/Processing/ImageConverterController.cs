using Emgu.CV.Linemod;
using Smartproj.Utils;
using System;
using System.Linq;

namespace Smartproj
{
    public class ImageConverterController : AbstractController
    {
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        public override bool Start(object[] _settings)
        {
            if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (Enabled)
            {
                StartParameters = _settings;
                Job job = (Job)StartParameters[0];
                WorkSpace ws = job.Owner.Owner.Owner;
                Log?.WriteInfo("ImageConverterController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал работу с процессом '{job.UID}'");

                ColorImagesConverter converter = new ColorImagesConverter();
                converter.ConverterLog = Log;
                converter.OutPath = job.JobPath;
                converter.ProfilesPath = ws.Profiles;
                try
                {
                    converter.Process(job.DataContainer);
                }
                catch (Exception ex)
                {
                    job.Status = ProcessStatusEnum.Error;
                    Log?.WriteError("ImageConverterController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Обработанное исключение. Процесс '{job.UID}' прерван '{ex.Message}'");
                    return false;
                }

                Log?.WriteInfo("ImageConverterController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
                return true;
            }

            return false;
        }
        protected override void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            }
            CurrentStatus = ProcessStatusEnum.Disposed;
        }
        public ImageConverterController() : base()
        {
            CurrentStatus = ProcessStatusEnum.New;
        }
    }
}
