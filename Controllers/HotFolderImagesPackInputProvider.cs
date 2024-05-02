using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smartproj
{
    public class HotFolderImagesPackInputProvider : AbstractController, IInputProvider
    {
        public string Source { get; set; }
        public override bool Activate(params object[] _settings)
        {
            if (!base.Activate(_settings))
            {
                return false;
            }

            if (Source == null || Source == "" || !File.Exists(Source) || Path.GetExtension(Source).ToLower() != ".zip")
            {
                Log?.WriteError("HotFolderImagesInputProvider.Activate", $"Ошибка при активации контроллера входных данных. Входной файл не определен, не существует, или имеет неподдерживаемый формат '{Source}'");
                StatusEnum = ControllerStatusEnum.Error;
                return false;
            }

            Job job = Owner?.Owner?.Owner;

            string tempPath = Path.Combine(job.Owner.ProjectPath, "~Original");
            Directory.CreateDirectory(tempPath);
            System.IO.File.Move(Source, Path.Combine(tempPath, Path.GetFileName(Source)));

            return true;
        }
        public override void Start()
        {
            throw new NotImplementedException();
        }
    }
}
