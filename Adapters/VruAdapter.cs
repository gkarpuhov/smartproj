using Smartproj.Utils;
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    public class VruAdapter : IAdapter
    {
        [XmlElement]
        public SourceParametersTypeEnum MetadataType { get; set; }
        [XmlElement]
        public TagFileTypeEnum FileDataFilter { get; set; }
        [XmlElement]
        public Guid UID { get; set; }
        public bool GetNext(Project _project, AbstractInputProvider _provider, out Job _job)
        {
            _job = null;
            if (_provider.Source == null || _provider.Source == "" || !Directory.Exists(_provider.Source))
            {
                _project.Log?.WriteError("VruAdapter.GetNext", $"{_project.ProjectId} => Входная директория не определена или не существует '{_provider.Source}'");
                return false;
            }

            var inputFiles = Directory.GetFiles(_provider.Source, "*.zip").OrderBy(x => (new FileInfo(x)).LastWriteTime);

            foreach (string file in inputFiles)
            {
                long filesize = FileProcess.CheckForProcessFile(file);
                if (filesize > 1024)
                {
                    _job = new Job(_project);
                    _job.Status = ProcessStatusEnum.Initalizing;
                    try
                    {
                        string origPath = Path.Combine(_job.JobPath, "~Original");
                        Directory.CreateDirectory(origPath);
                        string tempZip = Path.Combine(origPath, Path.GetFileName(file));
                        System.IO.File.Move(file, tempZip);
                        ZipFile.ExtractToDirectory(tempZip, origPath);
                        File.Delete(tempZip);

                        _project.Log?.WriteInfo("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: файлы для работы получены");
                    }
                    catch (Exception ex)
                    {
                        _project.Log?.WriteError("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: ошибка при извлечении файлов ({ex.Message})");
                        _job.Dispose();
                        _job = null;
                        return false;
                    }
                    // Разбор содержимого метадаты из извлеченных файлов
                    string metadata = "";
                    string[] allproducts = Directory.GetFiles(Path.Combine(_project.Home, "Products"), "*.xml", SearchOption.AllDirectories);
                    // Тут надо определить из исходных данных идентификатор продукта
                    string productId = "5c9f8e22-e5b5-40b5-ad20-b3758d190ee8"; // Например
                    string productFile = allproducts.SingleOrDefault(x => x.Contains(productId));
                    // Тут надо определить из исходных данных формат продукта
                    Size productSize = new Size(200, 280);  // Например
                    //
                    _project.Log?.WriteInfo("VruAdapter.GetNext", $"Продукт  {_project.ProjectId} => {productId} ({productSize}) передан процессу {_job.UID} для инициализации");

                    if (productFile != null && productFile != "")
                    {
                        try
                        {
                            _job.Create((Product)Serializer.LoadXml(productFile), productSize, metadata, MetadataType, FileDataFilter);
                            _project.Log?.WriteInfo("VruAdapter.GetNext", $"Продукт  {_project.ProjectId} => {productId} ({productSize}) успешно иницализирован процессом {_job.UID}");

                            return true;
                        }
                        catch (Exception ex)
                        {
                            _project.Log?.WriteError("VruAdapter.GetNext", $"Ошибка при загрузке продукта '{productFile}: {ex.Message}");
                            _project.Log?.WriteError("VruAdapter.GetNext", $"Ошибка при загрузке продукта '{productFile}: {ex.StackTrace}");
                        }
                    }

                    _project.Log?.WriteError("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: ошибка загрузки продукта {_project.ProjectId} => {productId}");
                    _job.Dispose();
                    _job = null;
                }
            }

            return false;
        }
        public VruAdapter() 
        {
            MetadataType = SourceParametersTypeEnum.XML;
            FileDataFilter = TagFileTypeEnum.JPEG;
            UID = Guid.NewGuid();
        }
    }
}
