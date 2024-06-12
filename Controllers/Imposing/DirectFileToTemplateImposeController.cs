using Emgu.CV.Features2D;
using Newtonsoft.Json.Linq;
using ProjectPage;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Контроллер осуществляет сборку макета только на основании содержимого исходных папок.
    /// 1.   Предполагается только два уровня вложенных папок - уровень детали и уровень шаблона.
    /// 2.   Имя вложенной папки соответстует коду детали. В ней содержаться подпапки, соответствуюшие шаблонам страниц.
    ///  2.1 Имя папки шаблона определяется по таблице псевдонимов, определенной для данного проекта. 
    ///  2.2 Если имя папки представляет собой идентификатор GUID, то сначала происходит попытка прямо обратиться к шаблону в системе по данному идентификатору.
    ///  2.3 Порядок страниц (использованных шаблонов) определяется добавление нумерации перед идентификатором шаблона, отделенной разделителем (\.|\s|_)+
    /// 3.   В папке шаблона страницы содержаться файлы изображений. Заполнение шаблона фотографиями происходит последовательно в алфавитном порядке.
    /// 4.   В папке может присутствовать файл с текстовой информацией (*.txt) с именем, совпадающем с именем изображения. В этом случае происходит попытка подтставления этого текста к связанному изображению
    /// </summary>
    public class DirectFileToTemplateImposeController : AbstractController
    {
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

                    Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал сборку макета... '{job.UID}'");

                    ExifTaggedFileSegments fsSegment = (ExifTaggedFileSegments)job.ProcessingSpace.Clusters[SegmentTypeEnum.FileStructure];

                    for (int i = 0; i < fsSegment.ChildNodes.Count; i++)
                    {
                        // Директории шаблонов
                        Segment directory = fsSegment.ChildNodes[i];

                        Match matchTempName = Regex.Match(directory.KeyId, @"\\(CVR|BLK|INS|FRZ)\\0*(\d+)[\.\s_]+([\w-]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

                        if (matchTempName.Success && job.Product.Parts.Any(x => String.Equals(x.KeyId, matchTempName.Groups[1].Value, StringComparison.OrdinalIgnoreCase)))
                        {
                            // Берем директории у которых пути соответствуют шаблону: \[Код детали]\[Порядковый номер][Разделитель][Уникальный GUID существующего шаблона или ссылка на него]

                            // 1. Логика работы данного контроллера: 1 сегмент соответствует 1 папке с файлами и 1 шаблону
                            // 2. Правильный порядок страниц формируем что помощью свойства OrderBy сегмента папки шаблона
                            // 3. Начальное значение данного свойства -1, автоматически порядок не выставляется. В свойство помещаем порядковый идентификатор из имени директории

                            if (directory.OrderBy == -1)
                            {
                                directory.OrderBy = int.Parse(matchTempName.Groups[2].Value);
                            }
                            else
                            {
                                if (directory.OrderBy != int.Parse(matchTempName.Groups[2].Value))
                                {
                                    Log.WriteError("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: Несколько директорий имеют один порядковый идентификатор {directory.OrderBy}; '{job.UID}'");
                                    job.Status = ProcessStatusEnum.Error;
                                    return;
                                }

                            }
                            ImposedDataContainer imposedlist = null;
                            if (!job.ProcessingSpace.OutData.TryGetValue(matchTempName.Groups[1].Value.ToUpper(), out imposedlist))
                            {
                                // Группировка по коду детали
                                imposedlist = new ImposedPdfDataContainer(job);
                                job.ProcessingSpace.OutData.Add(matchTempName.Groups[1].Value.ToUpper(), imposedlist);
                            }

                            Guid templid = default;
                            Template template = null;
                            if (Guid.TryParse(matchTempName.Groups[3].Value, out templid))
                            {
                                // Проверка что имя директории является правильным GUID
                                template = job.Product.LayoutSpace[job.ProductSize].TemplateCollection[templid];
                            }
                            if (template == null)
                            {
                                // Если имя не директории не GUID, проверяем является ли оно ссылкой на GUID шаблона
                                var alias = job.Owner.UsingTemplateAliases.Find(x => String.Equals(x.Name, matchTempName.Groups[3].Value, StringComparison.OrdinalIgnoreCase));
                                if (alias != null)
                                {
                                    template = job.Product.LayoutSpace[job.ProductSize].TemplateCollection[alias.UID];
                                }
                            }
                            if (template != null)
                            {
                                // Нужный шаблон успешно найден. Попытка заполнить файлами из директории сегмента
                                var toTryinmpse = imposedlist.Add(new ImposedLayout(template, directory));
                                if (TryImageImpose(toTryinmpse))
                                {

                                }
                                else
                                {
                                    Log.WriteError("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: Ошибка при заполнении шаблона '{template.UID}' источником '{directory.KeyId}'; '{job.UID}'");
                                    job.Status = ProcessStatusEnum.Error;
                                    return;
                                }
                            }
                            else
                            {
                                Log.WriteError("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: Не найден шаблон для источника '{directory.KeyId}'; '{job.UID}'");
                                job.Status = ProcessStatusEnum.Error;
                                return;
                            }
                        }
                    }

                    foreach (var partpages in job.ProcessingSpace.OutData)
                    {
                        partpages.Value.Sort((x, y) => x.Segments[0].OrderBy.CompareTo(y.Segments[0].OrderBy));
                    }

                    Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
                }
                finally
                {
                    CurrentStatus = ProcessStatusEnum.Finished;
                }
            }
            else
            {
                Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер деактивирован. Процессы не выполнены");
            }
        }
        private bool TryImageImpose(ImposedLayout _toTryinmpse)
        {
            var job = _toTryinmpse.Owner.Owner;
            List<int> data = new List<int>();
            for (int j = 0; j < _toTryinmpse.Segments[0].ChildNodes.Count; j++)
            {
                data.AddRange(_toTryinmpse.Segments[0].ChildNodes[j].Data);
            }

            int current = 0;
            for (int i = 0; i < _toTryinmpse.Templ.Frames.SidesCount; i++)
            {
                var frames = _toTryinmpse.Templ.Frames[i];
                for (int k = 0; k < frames.GetLength(0); k++)
                {
                    for (int m = 0; m < frames.GetLength(1); m++)
                    {
                        if (frames[k, m] != default && current < data.Count)
                        {
                            ImageFrame graphic = (ImageFrame)_toTryinmpse.Templ.Graphics.SingleOrDefault(x =>
                            {
                                if (x.GraphicType == GraphicTypeEnum.ImageFrame && (_toTryinmpse.Templ.Frames.SidesCount == 1 || (i == 0 && x.FrameSide == PageSide.Left) || (i == 1 && x.FrameSide == PageSide.Right)))
                                {
                                    return ((ImageFrame)x).FrameID.X == k && ((ImageFrame)x).FrameID.Y == m;
                                }
                                return false;
                            });
                            if (graphic == null) return false;

                            int fileid = data[current++];

                            _toTryinmpse.Imposed[i][k, m].FileId = fileid;
                            _toTryinmpse.Imposed[i][k, m].Owner = _toTryinmpse;

                            TryImageFit(graphic, _toTryinmpse.Imposed[i][k, m]);

                            job.InputDataContainer[fileid].AddStatus(ImageStatusEnum.Imposed);
                        }
                    }
                }
            }

            return _toTryinmpse.Available == 0;
        }
        protected override void Dispose(bool _disposing)
        {

        }
        public DirectFileToTemplateImposeController() : base()
        {
            CurrentStatus = ProcessStatusEnum.New;
        }
    }
}
