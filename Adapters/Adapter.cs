using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using YandexDisk.Client.Http;
using YandexDisk.Client.Protocol;

namespace Smartproj
{
    /// <summary>
    /// Контейнер параметров для получения данных из определенной файловой папки
    /// </summary>
    public class FileSource
    {
        [XmlCollection(false, false, typeof(string))]
        public List<string> Path { get; set; }
        [XmlElement]
        public string Mask { get; set; }
        [XmlElement]
        public int Priority { get; set; }
        public FileSource ()
        {
            Path = new List<string>();
            Mask = "";
            Priority = 1;
        }
    }
    /// <summary>
    /// Контейнер параметров для получения данных из произвольного удаленного источника
    /// </summary>
    public class RemoteSource
    {
        [XmlCollection(false, false, typeof(string))]
        public List<string> Link { get; set; }
        [XmlElement]
        public string Mask { get; set; }
        [XmlElement]
        public int Priority { get; set; }
        [XmlElement]
        public string Auth { get; set; }
        [XmlElement]
        public int Timeout { get; set; }
        [XmlElement]
        public string Parameters { get; set; }
        public RemoteSource()
        {
            Link = new List<string>();
            Mask = "";
            Priority = 0;
            Auth = "";
            Timeout = 0;
        }
    }
    /// <summary>
    /// Абстрактный адаптер для получения данных из файловой папки или хранилища Yandex Disc
    /// </summary>
    public abstract class Adapter : IAdapter
    {
        /// <summary>
        /// Можно определить для адаптера конкретный источник получения данных из папки. Актуально, если адаптер является специфическим для определенного клиента или типа продукции.
        /// Если данный параеметр определен, то он является приоритетным. В случае отсутствия, в качестве источника берется значение свойства <see cref="AbstractInputProvider.Source"/>
        /// </summary>
        [XmlContainer]
        public FileSource Local { get; set; }
        /// <summary>
        /// Можно определить для адаптера конкретный источник получения данных из удаленного источника. Актуально, если адаптер является специфическим для определенного клиента или типа продукции.
        /// Если данный параеметр определен, то он является приоритетным. В случае отсутствия, в качестве источника берется значение свойства <see cref="AbstractInputProvider.Source"/>
        /// </summary>
        [XmlContainer]
        public RemoteSource Remote { get; set; }
        [XmlElement]
        public SourceParametersTypeEnum MetadataType { get; set; }
        [XmlElement]
        public TagFileTypeEnum FileDataFilter { get; set; }
        [XmlElement]
        public Guid UID { get; set; }
        protected virtual IEnumerable<object> GetRemote(RemoteSource _input, AbstractInputProvider _provider)
        {
            return new List<object>();
        }
        protected virtual bool SetData(Project _project, AbstractInputProvider _provider, Job _job, object _sourceobject)
        {
            return false;
        }
        public bool GetNext(Project _project, AbstractInputProvider _provider, out Job _job)
        {
            _job = null;
            List<object> inputData = new List<object>();
            int inputType = 0;

            if (Local != null && Local.Path != null && Local.Path.Count > 0)
            {
                foreach (var dir in Local.Path)
                {
                    if (dir != "" && !Directory.Exists(dir))
                    {
                        _project.Log?.WriteError("Adapter.GetNext", $"{_project.ProjectId} => Входная директория не определена или не существует '{dir}'");
                        return false;
                    }
                }
                foreach (var dir in Local.Path)
                {
                    if (dir != "") inputData.AddRange(Directory.GetFiles(dir, Local.Mask != "" ? Local.Mask : "*.zip"));
                }

                if (inputData.Count > 0)
                {
                    inputData.Sort((x, y) =>
                    {
                        FileInfo infoX = new FileInfo((string)x);
                        FileInfo infoY = new FileInfo((string)y);
                        return infoX.LastWriteTime.CompareTo(infoY.LastWriteTime);
                    });
                }
            }

            if (inputData.Count == 0)
            {
                if (Remote != null && Remote.Link != null && Remote.Link.Count > 0)
                {
                    inputData.AddRange(GetRemote(Remote, _provider));
                    if (inputData.Count > 0)
                    {
                        inputData.Sort((x, y) => ((Resource)x).Modified.CompareTo(((Resource)y).Modified));
                    }
                    inputType = 1;
                }
            }

            if (inputData.Count == 0)
            {
                if (_provider.Source != null && _provider.Source != "")
                {
                    if (!Directory.Exists(_provider.Source))
                    {
                        _project.Log?.WriteError("Adapter.GetNext", $"{_project.ProjectId} => Входная директория не определена или не существует '{_provider.Source}'");
                        return false;
                    }

                    inputData.AddRange(Directory.GetFiles(_provider.Source, "*.zip"));

                    if (inputData.Count > 0)
                    {
                        inputData.Sort((x, y) =>
                        {
                            FileInfo infoX = new FileInfo((string)x);
                            FileInfo infoY = new FileInfo((string)y);
                            return infoX.LastWriteTime.CompareTo(infoY.LastWriteTime);
                        });
                    }
                }
            }

            if (inputData.Count() == 0)
            {
                return false;
            }

            foreach (object sourceobject in inputData)
            {
                string inputfile = "";

                switch (inputType)
                {
                    case 1:
                        {
                            string downloadsPath = Path.Combine(WorkSpace.ApplicationPath, "Temp");

                            using (var API = new DiskHttpApi(Remote.Auth))
                            {
                                var getLinkTask = API.Files.GetDownloadLinkAsync(((Resource)sourceobject).Path);

                                if (!getLinkTask.Wait(Remote.Timeout))
                                {
                                    _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => Превышено время ожидания ответа от сервера GetDownloadLinkAsync");
                                    continue;
                                }
                                if (getLinkTask.Result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                                {
                                    _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => GetDownloadLinkAsync Error -> {getLinkTask.Result.HttpStatusCode}");
                                    continue;
                                }

                                try
                                {
                                    var getFileTask = API.Files.DownloadAsync(getLinkTask.Result);

                                    if (!getFileTask.Wait(Remote.Timeout))
                                    {
                                        _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => Превышено время ожидания ответа от сервера DownloadAsync");
                                        continue;
                                    }

                                    using (var file = getFileTask.Result)
                                    {
                                        if (!getFileTask.IsFaulted && getFileTask.Exception == null && file != null)
                                        {
                                            inputfile = Path.Combine(downloadsPath, Guid.NewGuid().ToString());

                                            using (FileStream sr = new FileStream(inputfile, FileMode.Create, FileAccess.ReadWrite))
                                            {
                                                file.CopyTo(sr);
                                            }

                                            var dtask = API.Commands.DeleteAsync(new DeleteFileRequest { Path = ((Resource)sourceobject).Path, Permanently = true });
                                            if (!dtask.Wait(Remote.Timeout))
                                            {
                                                _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => Превышено время ожидания ответа от сервера DeleteAsync");
                                                continue;
                                            }
                                        }
                                        else
                                        {
                                            string message = getFileTask.Exception != null ? getFileTask.Exception.Message : "Неизвестная ошибка";
                                            _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => DownloadAsync Error -> {message}");
                                            continue;
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => Обработанное исключение при скачивании файла -> {e.Message}");
                                    continue;
                                }
                            }
                        }
                        break;
                    case 0:
                        {
                            inputfile = (string)sourceobject;
                            long filesize = FileProcess.CheckForProcessFile(inputfile);
                            if (filesize < 1024) continue;
                            if (Path.GetExtension(inputfile).ToLower() != ".zip") continue;
                            // В качестве файлов источника пока поддерживаются только архивы
                        }
                        break;
                    default:
                        _project.Log?.WriteError("Adapter.GetNext", $"Процесс {_project.ProjectId} => Неизвестный тип источника данных '{inputType}'");
                        return false;
                }

                _job = new Job(_project);
                _job.Status = ProcessStatusEnum.Initalizing;

                try
                {
                    string origPath = Path.Combine(_job.JobPath, "~Original");
                    Directory.CreateDirectory(origPath);
                    string tempZip = Path.Combine(origPath, Path.GetFileName(inputfile));
                    File.Move(inputfile, tempZip);
                    ZipFile.ExtractToDirectory(tempZip, origPath);
                    File.Delete(tempZip);

                    if (SetData(_project, _provider, _job, sourceobject))
                    {
                        _project.Log?.WriteInfo("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: файлы для работы получены");
                        return true;
                    }
                    else
                    {
                        _job.Dispose();
                        _job = null;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _project.Log?.WriteError("VruAdapter.GetNext", $"Процесс {_project.ProjectId} => {_job.UID}: ошибка при извлечении файлов ({ex.Message})");
                    _job.Dispose();
                    _job = null;
                    return false;
                }
            }

            return false;
        }
        protected Adapter()
        {
            MetadataType = SourceParametersTypeEnum.XML;
            FileDataFilter = TagFileTypeEnum.JPEG;
            UID = Guid.NewGuid();
        }
    }

}
