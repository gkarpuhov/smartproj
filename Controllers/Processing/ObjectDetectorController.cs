using Smartproj.Utils;
using Smartproj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Dpm;
using System.IO;

namespace Smartproj
{
    public class ObjectDetectorController : AbstractController
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
                Log?.WriteInfo("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал начал работу с процессом '{job.UID}'");

                ObjectDetect detector = new ObjectDetect();
                detector.DetectLog = Log;
                detector.ObjectDetectType = ObjectDetectImageEnum.FrontFace | ObjectDetectImageEnum.ProfileFace;
                detector.CascadesPath = Path.Combine(ws.MLData, "haarcascades");
                try
                {
                    detector.Detect(job.DataContainer, x => Path.Combine(job.JobPath, "~Files", x.GUID + ".jpg"));
                }
                catch (Exception ex)
                {
                    job.Status = ProcessStatusEnum.Error;
                    Log?.WriteError("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Обработанное исключение. Процесс '{job.UID}' прерван '{ex.Message}'");
                    return false;
                }

                Log?.WriteInfo("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
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
        public ObjectDetectorController() : base()
        {
            CurrentStatus = ProcessStatusEnum.New;
        }
    }
}
