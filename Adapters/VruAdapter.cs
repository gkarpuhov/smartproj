using Smartproj;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace smartproj.Adapters
{
    public class VruAdapter : IAdapter
    {
        public AbstractInputProvider Owner { get; }

        public SourceParametersTypeEnum ParametersType => SourceParametersTypeEnum.XML;

        public TagFileTypeEnum FileDataFilter => TagFileTypeEnum.JPEG;
        public bool GetNext(Job _job, out string _link, out string _data)
        {
            _link = ""; _data = "";
            if (Owner?.Source == null || Owner.Source == "" || !Directory.Exists(Owner.Source))
            {
                return false;
            }

            var inputFiles = Directory.GetFiles(Owner.Source, "*.zip").OrderBy(x => (new FileInfo(x)).LastWriteTime);
            foreach (string file in inputFiles)
            {
                long filesize = FileProcess.CheckForProcessFile(file);
                if (filesize > 1024)
                {
                    try
                    {
                        string origPath = Path.Combine(_job.JobPath, "~Original");
                        Directory.CreateDirectory(origPath);
                        string tempZip = Path.Combine(origPath, Path.GetFileName(file));
                        System.IO.File.Move(file, tempZip);
                        string zipDir = Path.Combine(origPath, Path.GetFileNameWithoutExtension(tempZip));
                        Directory.CreateDirectory(zipDir);
                        ZipFile.ExtractToDirectory(tempZip, zipDir);
                        File.Delete(tempZip);
                        _link = zipDir;
                        Owner.Log?.WriteInfo("VruAdapter.GetNext", $"Процесс {_job.UID}: файлы для работы получены");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Owner.Log?.WriteError("VruAdapter.GetNext", $"Процесс {_job.UID}: ошибка при извлечении файлов ({ex.Message})");
                        return false;
                    }
                }
            }

            return false;
        }
        public VruAdapter(AbstractInputProvider _owner) 
        {
            Owner = _owner;
        }
    }
}
