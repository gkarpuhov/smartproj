using Smartproj.Utils;
using Smartproj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV.Dpm;
using System.IO;
using System.Xml.Serialization;

namespace Smartproj
{
    public class ObjectDetectorController : AbstractController
    {
        [XmlElement]
        public int SampleSize { get; set; }
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        public override void Start(object[] _settings)
        {
            if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (Enabled)
            {
                CurrentStatus = ProcessStatusEnum.Processing;
                Job job = (Job)_settings[0];

                try
                {
                    StartParameters = _settings;
                    WorkSpace ws = job.Owner.Owner.Owner;
                    Log?.WriteInfo("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал начал работу с процессом '{job.UID}'");

                    ObjectDetect detector = new ObjectDetect();
                    detector.DetectLog = Log;
                    detector.SampleSize = SampleSize;
                    detector.ObjectDetectType = ObjectDetectImageEnum.FrontFace | ObjectDetectImageEnum.ProfileFace;
                    detector.CascadesPath = Path.Combine(ws.MLData, "haarcascades");

                    if (!detector.Detect(job.InputDataContainer, x => Path.Combine(job.JobPath, "~Files", x.GUID + (job.Product.Optimization == FileSizeOptimization.Lossless ? ".tiff" : ".jpeg")), job.ProcessingSpace.ObjectDetectedAreas))
                    {
                        job.Status = ProcessStatusEnum.Error;
                        Log?.WriteError("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Ошибка при выполненнии процесса '{job.UID}'");
                    }
                    else
                    {
                        Log?.WriteInfo("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
                    }
                }
                catch (Exception ex)
                {
                    job.Status = ProcessStatusEnum.Error;
                    Log?.WriteError("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Обработанное исключение. Процесс '{job.UID}' прерван '{ex.Message}'");
                }
                finally
                {
                    CurrentStatus = ProcessStatusEnum.Finished;
                }
            }
            else
            {
                Log?.WriteInfo("ObjectDetectorController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер деактивирован. Процессы не выполнены");
            }
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
            SampleSize = 2000;
        }
    }
}
