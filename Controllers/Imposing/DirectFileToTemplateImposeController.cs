using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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
        public override bool Start(object[] _settings)
        {
            if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (Enabled)
            {
                StartParameters = _settings;
                Job job = (Job)StartParameters[0];
                WorkSpace ws = job.Owner.Owner.Owner;
                Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал сборку макета... '{job.UID}'");

                string jobFiles = Path.Combine(job.JobPath, "~Original");
                Dictionary<string, List<ValueTuple<int, Template, IEnumerable<ExifTaggedFile>, string[]>>> datacache = new Dictionary<string, List<(int, Template, IEnumerable<ExifTaggedFile>, string[])>>();
                
                foreach (var detail in job.Product.Parts)
                {
                    if (datacache.ContainsKey(detail.KeyId)) continue;

                    string part = Path.Combine(jobFiles, detail.KeyId);

                    if (Directory.Exists(part))
                    {
                        List<ValueTuple<int, Template, IEnumerable<ExifTaggedFile>, string[]>> pagesdata = new List<(int, Template, IEnumerable<ExifTaggedFile>, string[])>();
                        datacache.Add(detail.KeyId, pagesdata);

                        string[] pages = Directory.GetDirectories(part);
                        for (int i = 0; i < pages.Length; i++)
                        {
                            Match matchTempName = Regex.Match(Path.GetFileName(pages[i]), @"^0*(\d+)[\.\s_]+(.+)", RegexOptions.Compiled);
                            if (matchTempName.Success) 
                            {
                                Guid templid = default;
                                Template template = null;
                                if (Guid.TryParse(matchTempName.Groups[2].Value, out templid))
                                {
                                    template = job.Product.LayoutSpace[job.ProductSize].TemplateCollection[templid];
                                }
                                if (template == null)
                                {
                                    var alias = job.Owner.UsingTemplateAliases.Find(x => String.Equals(x.Name, matchTempName.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
                                    if (alias != null)
                                    {
                                        template = job.Product.LayoutSpace[job.ProductSize].TemplateCollection[alias.UID];
                                    }
                                }
                                if (template != null)
                                {
                                    var images = job.DataContainer.Where(x => String.Equals(x.FilePath, pages[i], StringComparison.OrdinalIgnoreCase));
                                    var texts = Directory.GetFiles(pages[i], "*.txt", SearchOption.TopDirectoryOnly);

                                    pagesdata.Add(new ValueTuple<int, Template, IEnumerable<ExifTaggedFile>, string[]>(int.Parse(matchTempName.Groups[1].Value), template, images, texts));
                                }
                            }
                        }
                    }
                }

                //if (datacache.Sum(x => x.Value.Sum(y => y.Item3.Count())) > 0) 
                {
                    foreach (var pair in datacache)
                    {
                        foreach (var item in pair.Value)
                        {
                            Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"Detail = {pair.Key}; Order = {item.Item1}; Template = {item.Item2.UID}");
                            foreach (var file in item.Item3)
                            {
                                Log?.WriteInfo("DirectFileToTemplateImposeController.Start", $"File Name = {file.FileName}; File ID = {file.GUID}");
                            }
                        }
                    }

                    return true;
                }
            }
            return false;
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
