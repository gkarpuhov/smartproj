using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Xml.Serialization;

namespace Smartproj
{
    public class ProjectCollection : IEnumerable<Project>
    {
        private List<Project> mItems;
        public WorkSpace Owner { get; }
        public Project this[int _index] => mItems[_index];
        public Project this[string _index] => mItems.Find(x => String.Compare(x.ProjectId, _index, StringComparison.OrdinalIgnoreCase) == 0);
        public IEnumerator<Project> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        public Project Add(Project _item)
        {
            _item.Owner = this;
            mItems.Add(_item);
            return _item;
        }
        public void Clear()
        {
            foreach (var item in mItems)
            {
                item.Owner = null;  
            }
            mItems.Clear(); 
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        public ProjectCollection(WorkSpace _owner)
        {
            Owner = _owner;
            mItems = new List<Project>();
        }
    }
    public partial class Project : IDisposable
    {
        private bool mIsDisposed;
        public readonly string ProjectPath;
        public readonly Logger Log;
        public string Home => Path.Combine(Owner.Owner.Config, ProjectId);
        public ProjectCollection Owner { get; set; }
        [XmlElement]
        public bool ProductsAutoUpdate { get; set; }
        //public ProductCollection Products { get; set; }
        [XmlElement]
        public Guid UID { get; set; }
        //[XmlCollection(false, false, typeof(Guid))]
        //public List<Guid> ProductKeys { get; set; }
        [XmlElement]
        public string ProjectId { get; set; }
        //
        /*
        internal void CreateProducts()
        {
            Products = new ProductCollection(this);

            string[] files = Directory.GetFiles(Path.Combine(Home, "Products"), "*.xml", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                try
                {
                    Product product = (Product)Serializer.LoadXml(file);

                    if (ProductsAutoUpdate && !ProductKeys.Contains(product.UID))
                    {
                        ProductKeys.Add(product.UID);
                    }

                    if (ProductKeys.Contains(product.UID))
                    {
                        if (ProductsAutoUpdate)
                        {
                            product.TemplateKeys.Clear();
                        }

                        Products.Add(product);

                        if (ProductsAutoUpdate)
                        {
                            product.Save();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteError("CreateProducts", $"Ошибка при загрузке продукта '{file}: {ex.Message}");
                    Log.WriteError("CreateProducts", $"Ошибка при загрузке продукта '{file}: {ex.StackTrace}");
                }   
            }
        }
        */
        protected Project() : this("") { }
        public Project(string _cid)
        {
            mIsDisposed = false;
            ProjectId = _cid;
            UID = Guid.NewGuid();
            ProjectPath = Path.Combine(WorkSpace.WorkingPath, "Temp", UID.ToString());
            //ProductKeys = new List<Guid>();
            Directory.CreateDirectory(ProjectPath);
            Directory.CreateDirectory(Path.Combine(ProjectPath, "~Files"));
            Directory.CreateDirectory(Path.Combine(ProjectPath, "~Cms"));
            ProductsAutoUpdate = false;
            Log = new Logger();
            Log.Open(Path.Combine(ProjectPath, "log.txt"));
        }
        /*
        public void ExtractData(List<KeyValuePair<string, List<string>>> _filesTree)
        {
            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();

            InputData.Clear();

            var actualTemplates = LayoutSpace.Find(x => (x.Key.Width == 200 && x.Key.Height == 280)).Value.TemplateCollection;

            var extractor = new ExifTool(new ExifToolOptions() { EscapeTagValues = false, ExtractICCProfile = true });

            ExifTaggedFileSegments topSegment = Clusters;
            ExifTaggedFileSegments fsSegment = (ExifTaggedFileSegments)topSegment.Add(SegmentTypeEnum.FileStructure, "0");

            var allFrames = actualTemplates.GetFrameIndexes();
            var allAreas = actualTemplates.GetFrameAreas();

            int minarea = allAreas.Keys.Min(x => x.Width * x.Height);
            int criticalPixels = (int)(minarea * (MinResolution * MinResolution / 645.16f));

            Log.WriteInfo("ExtractData", $"minarea = {minarea}; criticalPixels = {criticalPixels}; MinResolution = {MinResolution}; count = {actualTemplates.Count()}; allFrames = {allFrames.Count}; allAreas = {allAreas.Count}");

            int index = 0;

            for (int i = 0; i < _filesTree.Count; i++)
            {
                var list = extractor.ExtractAllAsync(_filesTree[i].Key);
                list.Wait();

                List<ExifTaggedFile> extractedList = new List<ExifTaggedFile>();

                for (int j = 0; j < _filesTree[i].Value.Count; j++)
                {
                    IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> tags = null;
                    string key = Path.Combine(_filesTree[i].Key, _filesTree[i].Value[j]).Replace("\\", "/");

                    if (list.Result.TryGetValue(key, out tags))
                    {

                        string[] groupsOrder = new string[7] { "File", "EXIF", "JFIF", "XMP", "Composite", "PNG", "QuickTime" };

                        ValueTuple<string, string, string> FindValue(IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> _tags, string _name, bool _exactName, params string[] _groupsName)
                        {
                            var outValue = new ValueTuple<string, string, string>("", "", "");

                            for (int f = 0; f < _groupsName.Length; f++)
                            {
                                foreach (var grouptags in _tags)
                                {
                                    if (grouptags.Key == _groupsName[f])
                                    {
                                        foreach (var prop in grouptags.Value)
                                        {
                                            string[] namePair = prop.Name.Split(':');
                                            if (namePair.Length == 2 && namePair[0] == _groupsName[f] && ((_exactName && namePair[1] == _name) || (!_exactName && namePair[1].Contains(_name))))
                                            {
                                                outValue.Item1 = prop.Value["val"].ToString();
                                                outValue.Item2 = prop.Value["num"]?.ToString();
                                                outValue.Item3 = _groupsName[f];
                                                return outValue;
                                            }
                                        }
                                    }
                                }
                            }
                            return outValue;
                        }

                        //
                        DateTime fileCreateDate = default;
                        ValueTuple<int, int> gps = default;
                        Size imageSize = default;
                        bool hasGps = false;
                        byte[] colorProfile = null;
                        byte samplesPerPixel = 0;
                        byte[] bitsPerSample = null, imagePixelDepth = null;
                        TagColorModeEnum colorMode = TagColorModeEnum.Unknown;
                        byte colorType = 10;
                        byte bitDepth = 0;
                        string mIMEType = "";
                        byte colorComponents = 0;

                        //
                        // Color Profile
                        string currentGroupKey = "ICC_Profile";
                        var findValue = FindValue(tags, currentGroupKey, true, "ICC_Profile");
                        if (findValue.Item1 != null && findValue.Item1.Length > 7)
                        {
                            colorProfile = System.Convert.FromBase64String(findValue.Item1.Substring(7, findValue.Item1.Length - 7));
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // CreateDate
                        currentGroupKey = "ModifyDate";
                        System.Text.RegularExpressions.Match match = null;
                        findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                        if (findValue.Item3 != "" && (match = Regex.Match(findValue.Item1, @"(\d{4}):(\d{2}):(\d{2})\s+(\d{2}):(\d{2}):(\d{2})([^\d]|$)", RegexOptions.Compiled)).Success)
                        {
                            fileCreateDate = new DateTime(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value), int.Parse(match.Groups[3].Value), int.Parse(match.Groups[4].Value), int.Parse(match.Groups[5].Value), int.Parse(match.Groups[6].Value));
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // SamplesPerPixel
                        currentGroupKey = "SamplesPerPixel";
                        findValue = FindValue(tags, currentGroupKey, true, groupsOrder);
                        if (findValue.Item3 != "" && byte.TryParse(findValue.Item1, out samplesPerPixel))
                        {
                            //StreamLogger.WriteLine($"findValue.Item1 (OUT) =  {samplesPerPixel}: currentGroupKey = '{currentGroupKey}'");
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // BitsPerSample, ImagePixelDepth
                        currentGroupKey = "BitsPerSample";
                        MatchCollection matches = null;
                        findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                        if (findValue.Item3 != "" && (matches = Regex.Matches(findValue.Item1, @"(\d+)+", RegexOptions.Compiled)).Count > 0)
                        {
                            bitsPerSample = new byte[matches.Count];
                            for (int m = 0; m < matches.Count; m++)
                            {
                                bitsPerSample[m] = byte.Parse(matches[m].Value);
                            }
                            //StreamLogger.WriteLine($"findValue.Item1 (OUT) =  {String.Join(" ", bitsPerSample)}: currentGroupKey = '{currentGroupKey}'");
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        currentGroupKey = "ImagePixelDepth";
                        matches = null;
                        findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                        if (findValue.Item3 != "" && (matches = Regex.Matches(findValue.Item1, @"(\d+)+", RegexOptions.Compiled)).Count > 0)
                        {
                            imagePixelDepth = new byte[matches.Count];
                            for (int m = 0; m < matches.Count; m++)
                            {
                                imagePixelDepth[m] = byte.Parse(matches[m].Value);
                            }
                            //StreamLogger.WriteLine($"findValue.Item1 (OUT) =  {String.Join(" ", imagePixelDepth)}: currentGroupKey = '{currentGroupKey}'");
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // ColorMode
                        currentGroupKey = "ColorMode";
                        findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                        if (findValue.Item3 != "" && findValue.Item1 != "" && Enum.TryParse(findValue.Item1, out colorMode))
                        {
                            //
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // ColorType
                        currentGroupKey = "ColorType";
                        findValue = FindValue(tags, currentGroupKey, true, "PNG");
                        if (findValue.Item3 != "" && byte.TryParse(findValue.Item2, out colorType))
                        {
                            // PNG
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // BitDepth
                        currentGroupKey = "BitDepth";
                        findValue = FindValue(tags, currentGroupKey, true, "PNG");
                        if (findValue.Item3 != "" && byte.TryParse(findValue.Item1, out bitDepth))
                        {
                            // PNG
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // ColorComponents
                        currentGroupKey = "ColorComponents";
                        findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                        if (findValue.Item3 != "" && byte.TryParse(findValue.Item1, out colorComponents))
                        {
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // MIMEType
                        currentGroupKey = "MIMEType";
                        findValue = FindValue(tags, currentGroupKey, true, "File");
                        if (findValue.Item3 != "")
                        {
                            mIMEType = findValue.Item1;
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // "GPSLatitude",
                        // "GPSLongitude",
                        currentGroupKey = "GPSLatitude";
                        findValue = FindValue(tags, currentGroupKey, true, groupsOrder);
                        if (findValue.Item3 != "" && findValue.Item2 != null)
                        {
                            float latitude;
                            if (float.TryParse(findValue.Item2.Replace(",", "."), NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." }, out latitude))
                            {
                                gps.Item1 = (int)(latitude * 10000000);

                                currentGroupKey = "GPSLongitude";
                                findValue = FindValue(tags, currentGroupKey, true, groupsOrder);
                                if (findValue.Item3 != "" && findValue.Item2 != null)
                                {
                                    float longitude;
                                    if (float.TryParse(findValue.Item2.Replace(",", "."), NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." }, out longitude))
                                    {
                                        gps.Item2 = (int)(longitude * 10000000);
                                        hasGps = true;
                                    }
                                }
                                else
                                {
                                    //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                                }
                            }
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }
                        // "ImageWidth",
                        // "ImageHeight",
                        currentGroupKey = "ImageWidth";
                        findValue = FindValue(tags, currentGroupKey, true, groupsOrder);
                        if (findValue.Item3 != "")
                        {
                            int w = default;
                            if (int.TryParse(findValue.Item1, out w))
                            {
                                currentGroupKey = "ImageHeight";
                                findValue = FindValue(tags, currentGroupKey, true, groupsOrder);
                                if (findValue.Item3 != "")
                                {
                                    int h = default;
                                    if (int.TryParse(findValue.Item1, out h))
                                    {
                                        imageSize = new Size(w, h);
                                    }
                                }
                                else
                                {
                                    //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                                }
                            }
                        }
                        else
                        {
                            //StreamLogger.WriteLine($"!!! {_filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                        }

                        TagFileTypeEnum fileNameType = _filesTree[i].Value[j].ToFileType();
                        TagMIMETypeEnum fileMimeType = mIMEType.ToMIMEType();

                        if (fileNameType != TagFileTypeEnum.UNDEFINED)
                        {
                            if (fileNameType.ToString() != fileMimeType.ToString() && fileNameType != TagFileTypeEnum.JFIF)
                            {
                                errors.Add($"{_filesTree[i].Value[j]}: Не определен формат файла");
                                continue;
                            }
                        }
                        else
                        {
                            if (fileMimeType != TagMIMETypeEnum.UNDEFINED)
                            {
                                Enum.TryParse(fileMimeType.ToString(), out fileNameType);
                            }
                        }

                        if ((FileTypeFilter & fileNameType) != fileNameType)
                        {
                            errors.Add($"{_filesTree[i].Value[j]}: Не определен формат файла");
                            continue;
                        }

                        bool hasAlpha = false;
                        byte bitsPerSampleValue = 0;

                        if (fileMimeType == TagMIMETypeEnum.PNG)
                        {
                            // PNG
                            //0 = Grayscale
                            //2 = RGB
                            //3 = Palette
                            //4 = Grayscale with Alpha
                            //6 = RGB with Alpha

                            switch (colorType)
                            {
                                case 0: colorMode = TagColorModeEnum.Grayscale; break;
                                case 2: colorMode = TagColorModeEnum.RGB; break;
                                case 4: colorMode = TagColorModeEnum.Grayscale; hasAlpha = true; break;
                                case 6: colorMode = TagColorModeEnum.RGB; hasAlpha = true; break;
                            }

                            if (bitDepth > 0) bitsPerSampleValue = bitDepth;
                        }

                        if (colorMode == TagColorModeEnum.Unknown)
                        {
                            switch (colorComponents)
                            {
                                case 1: colorMode = TagColorModeEnum.Grayscale; break;
                                case 3: colorMode = TagColorModeEnum.RGB; break;
                                case 4: colorMode = TagColorModeEnum.CMYK; break;
                            }
                        }
                        if (colorMode == TagColorModeEnum.Unknown)
                        {
                            switch (samplesPerPixel)
                            {
                                case 1: colorMode = TagColorModeEnum.Grayscale; break;
                                case 3: colorMode = TagColorModeEnum.RGB; break;
                                case 4: colorMode = TagColorModeEnum.CMYK; break;
                                default:
                                    errors.Add($"{_filesTree[i].Value[j]}: Не определен формат цветового пространства");
                                    continue;
                            }
                        }

                        if (bitsPerSample != null && imagePixelDepth != null && bitsPerSample.Length != 0 && imagePixelDepth.Length != 0)
                        {
                            if (bitsPerSample[0] != imagePixelDepth[0])
                            {
                                errors.Add($"{_filesTree[i].Value[j]}: Ошибка в значении BitsPerSample или ImagePixelDepth");
                                continue;
                            }
                        }

                        if (bitsPerSample != null && bitsPerSample.Length > 0) bitsPerSampleValue = bitsPerSample[0];
                        if (imagePixelDepth != null && imagePixelDepth.Length > 0) bitsPerSampleValue = imagePixelDepth[0];


                        if (bitsPerSampleValue == 0)
                        {
                            errors.Add($"{_filesTree[i].Value[j]}: Не определено значение 'BitsPerSample'");
                            continue;
                        }

                        if (criticalPixels < imageSize.Width * imageSize.Height)
                        {
                            ExifTaggedFile newitem1 = new ExifTaggedFile(index++, _filesTree[i].Value[j], _filesTree[i].Key, imageSize, fileCreateDate, fileNameType, tags, colorMode, hasGps, gps, colorProfile, bitsPerSampleValue, false, hasAlpha, false);
                            extractedList.Add(newitem1);
                            newitem1.AddStatus(ImageStatusEnum.ExifData);
                            InputData.Add(newitem1);
                        }
                        else
                        {
                            warnings.Add($"Недостаточный размер изображения {imageSize.Width}x{imageSize.Height}. Файл '{_filesTree[i].Value[j]}' будет проигнорирован");
                        }
                    }
                }

                if (extractedList.Count > 0)
                {
                    fsSegment.ImportFiles(_filesTree[i].Key, extractedList, x => Path.GetFileNameWithoutExtension(x.FileName), x => x.Index);
                }
            }

            Log.WriteAll("Параметры Exif данных", messages, warnings, errors);
            messages.Clear();
            warnings.Clear();
            errors.Clear();

            if (InputData.Count > 0)
            {
                // Сквозной порядковый индекс по возрастанию в соответствии со структурой сегментов
                // И сохраняем в плоском списке
                List<Segment> flatFilesTree = new List<Segment>();
                int ordercounter = 0;
                for (int i = 0; i < fsSegment.TreeNodeItems.Count; i++)
                {
                    var directory = fsSegment.TreeNodeItems[i];
                    for (int j = 0; j < directory.TreeNodeItems.Count; j++)
                    {
                        List<int> data = (List<int>)directory.TreeNodeItems[j].Data;
                        for (int k = 0; k < data.Count; k++)
                        {
                            InputData[data[k]].OrderBy = ordercounter++;
                        }
                        // Меняем список на хеш-контейнер для более быстрой обработки
                        directory.TreeNodeItems[j].Data = new HashSet<int>(directory.TreeNodeItems[j].Data);
                        flatFilesTree.Add(directory.TreeNodeItems[j]);
                    }
                }

                // Выделяем отдельно сегменты со ключом - звёздочка
                //var xSegments = flatFilesTree.Where(x => x.KeyId == "*").ToDictionary(key => key.Parent.KeyId, val => val);
                //var flatWithoutX = flatFilesTree.Where(x => x.KeyId != "*").ToList();
           
                InputNormalizer(InputData);
                ObjectDetect(InputData);

                //topSegment.ImportKmeansClusters(InputData.Where(x => x.CreateImageMinutes > 0).Select(x => x.CreateImageMinutes.ToDistance(x.Index)), SegmentTypeEnum.DateCreate, "DateInt", 100);
                //topSegment.ImportKmeansClusters(InputData.Where(x => x.GpsPosition != default).Select(x => (IDistance<int>) new PointI(x.GpsPosition.Item1, x.GpsPosition.Item2, x.Index)), SegmentTypeEnum.Geolocation, "GPS", 100);


                //var sortedTemplates = Product.TemplateKeys.OrderByDescending(x => x.Frames.FramesCount);
                var sortedTemplates = actualTemplates.OrderByDescending(x => x.Frames.FramesCount); ;

                Dictionary<Segment, List<ProjectContainer>> centrouds1d = new Dictionary<Segment, List<ProjectContainer>>();
                Dictionary<ValueTuple<Segment, Segment>, ProjectContainer> centrouds2d = new Dictionary<ValueTuple<Segment, Segment>, ProjectContainer>();

                List<ProjectContainer> fullImposed1d = new List<ProjectContainer>();
                List<ProjectContainer> fullImposed2d = new List<ProjectContainer>();

                HashSet<int> imposedHash1d = new HashSet<int>();
                HashSet<int> imposedHash2d = new HashSet<int>();

                float limit = 1.2f;
                int minFramesInTemplate = 0;
                int iteration = 0;
                var k2 = 0.8f;

                IEnumerable<ExifTaggedFile> files = InputData;
                List<ProjectContainer> step_1 = null;
                List<ProjectContainer> step_2 = null;

                while (true)
                {
                    var matrix = GetSizeMatrix(files, minFramesInTemplate, 0.8f, 1, k2, 0.0f);

                    messages.Add($" - minFramesInTemplate = {minFramesInTemplate}");

                    for (int m = 0; m < matrix.Count && matrix[m].index < limit; m++)
                    {
                        if (imposedHash1d.Contains(matrix[m].image_id)) continue;

                            for (int j = 0; j < flatFilesTree.Count; j++)
                            {
                                Segment segm = flatFilesTree[j];

                                if (segm.Data.Contains(matrix[m].image_id))
                                {
                                    // Нашли файл
                                    List<ProjectContainer> centroid = null;
                                    // ищеем в словаре группу по совпадению расположения (директория + логическая группа файлов)
                                    if (!centrouds1d.TryGetValue(segm, out centroid))
                                    {
                                        centroid = new List<ProjectContainer>();
                                        centrouds1d.Add(segm, centroid);
                                    }
                                    // Ищем в группе шаблон в котором есть свободый фрейм нужного размера
                                    ProjectContainer pp = null;
                                    Size size = allFrames[matrix[m].frame_id].Key;
                                    foreach (var item in centroid)
                                    {
                                        if (item.TryImageImpose(size, PageSide.Single, matrix[m].image_id))
                                        {
                                            pp = item;
                                            break;
                                        }
                                    }
                                    // Если такого шаблона не нашлось, то добавляем новый.
                                    // -- Количество шаблонов не должно превысить объем станиц в продукте
                                    if (pp == null)
                                    {
                                        foreach (var template in sortedTemplates)
                                        {
                                            if (template.Frames.FramesCount > minFramesInTemplate)
                                            {
                                                foreach (RectangleF rect in template.Frames)
                                                {
                                                    if (rect.Width.ToInt() == size.Width && rect.Height.ToInt() == size.Height)
                                                    {
                                                        pp = new ProjectContainer(this, template, 2, segm);
                                                        if (pp.TryImageImpose(size, PageSide.Single, matrix[m].image_id))
                                                        {
                                                            centroid.Add(pp);
                                                            break;
                                                        }
                                                        else
                                                        {
                                                            pp = null;
                                                        }
                                                    }
                                                }
                                                if (pp != null) break;
                                            }
                                        }
                                    }

                                    // Если шаблон полностью заполнен, переносим из коллекции
                                    if (pp != null)
                                    {
                                        if (pp.Available == 0)
                                        {
                                            centroid.Remove(pp);
                                            fullImposed1d.Add(pp);

                                            foreach (var id in pp)
                                            {
                                                imposedHash2d.Add(id.FileId);
                                            }
                                        }
                                        imposedHash1d.Add(matrix[m].image_id);
                                    }

                                    // Файл вставляем только один раз
                                    break;
                                }
                            }
                    }

                    // Имеем некоторое количество полностью заполненных шаблонов (в каждом данные из одного сегмента). Ну или не имеем, если не повезло
                    // Понятно, что в большинстве случаях соберется немного шаблонов, которых будет недостаточно для заполнения проекта
                    // Пробуем заполнить шаблоны данными из двух соседних сегментов

                    // В хэше все обработанные файлы. Оставляем только которые собрались в шаблоны, неполностью собранные удаляем
                    imposedHash1d.RemoveWhere(x => !imposedHash2d.Contains(x));

                    for (int m = 0; m < matrix.Count && matrix[m].index < limit; m++)
                    {
                        if (imposedHash1d.Contains(matrix[m].image_id)) continue;

                        for (int i = 1; i < flatFilesTree.Count; i++)
                        {
                            Segment segment1 = flatFilesTree[i - 1];
                            Segment segment2 = flatFilesTree[i];

                            // Пробуем в любое место вставлять файлы из неразобранных сегментов
                            // Даже если файл находится в одном из двух сегментов, в отличии от предыдущего алгоритма, не прекращаем повторный поиск данного файла далее
                            // Это даст большее число возможных комбинаций, но нужно в конечном наборе собранных шаблонов, отфильтровать с повторяющимися файлами
                            bool dataSegIsPresent = segment1.Data.Contains(matrix[m].image_id) || segment2.Data.Contains(matrix[m].image_id);

                            if (dataSegIsPresent)
                            {
                                ProjectContainer twoSegmContainer = null;
                                Size size = allFrames[matrix[m].frame_id].Key;
                                ValueTuple<Segment, Segment> segm = new ValueTuple<Segment, Segment>(segment1, segment2);

                                if (!centrouds2d.TryGetValue(segm, out twoSegmContainer))
                                {
                                    foreach (var template in sortedTemplates)
                                    {
                                        if (template.Frames.FramesCount > minFramesInTemplate)
                                        {
                                            foreach (RectangleF rect in template.Frames)
                                            {
                                                if (rect.Width.ToInt() == size.Width && rect.Height.ToInt() == size.Height)
                                                {
                                                    twoSegmContainer = new ProjectContainer(this, template, 2, segment1, segment2);
                                                    break;
                                                }
                                            }
                                        }
                                        if (twoSegmContainer != null) break;
                                    }

                                    centrouds2d.Add(segm, twoSegmContainer);
                                }

                                if (twoSegmContainer != null)
                                {
                                    if (twoSegmContainer.TryImageImpose(size, PageSide.Single, matrix[m].image_id))
                                    {
                                        bool checkedforadd = true;

                                        if (twoSegmContainer.Available == 0)
                                        {
                                            // Нужно посмотреть, не получился ли уже ранее шаблон между такими же сегментами. Он должен быть только один
                                            // Вообще, добавляем копию файла, если только он добавлится в контейнер с другой комбинацией сегментов
                                            foreach (var old in fullImposed2d)
                                            {
                                                if (twoSegmContainer.Segments.SequenceEqual(old.Segments))
                                                //if (twoSegmContainer.Segments.Contains(segment1) && old.Segments.Contains(segment1) && twoSegmContainer.Segments.Contains(segment2) && old.Segments.Contains(segment2))
                                                {
                                                    // Смотрим разницу в наполнении контейнеров файлами
                                                    var deltaImages = twoSegmContainer.Select(x => x.FileId).Except(old.Select(y => y.FileId));
                                                    // Если действительно такая ситуация случилась, берем контейнер с большим количеством фреймов
                                                    if (twoSegmContainer.Templ.Frames.FramesCount > old.Templ.Frames.FramesCount)
                                                    {
                                                        // 1. Заменяем старый новым. Удалить все данные старого
                                                        fullImposed2d.Remove(old);
                                                    }
                                                    else
                                                    {
                                                        // 2.
                                                        checkedforadd = false;
                                                    }
                                                    // Если есть разница в наполнении, нужно обеспечить логику корректной работы фильтрующего хэша imposedHash1d
                                                    // Нужно поискать, не присутстуют ли элементы этой разницы в незаполненных контейнерах. Если нет, можно удалить из хеша
                                                    foreach (var id in deltaImages)
                                                    {
                                                        if (!centrouds2d.Any(x => (x.Key != segm && x.Value.Any(y => y.FileId == id))))
                                                        {
                                                            imposedHash1d.Remove(id);
                                                            // из заполненных тоже удалить. если будет надо, далее добавится
                                                            imposedHash2d.Remove(id);
                                                        }
                                                    }

                                                    break;
                                                }
                                            }
                                            // Добавляем все данные нового контейнера. Исключение, если аналогичный контейнер уже был
                                            if (checkedforadd)
                                            {
                                                fullImposed2d.Add(twoSegmContainer);

                                                foreach (var id in twoSegmContainer)
                                                {
                                                    imposedHash2d.Add(id.FileId);
                                                }
                                            }
                                            // В любом случае удаляем из словаря неполностью заполненных контейнеров
                                            centrouds2d.Remove(segm);
                                        }

                                        // В фильтрующем хеше текушиее изображение в люом случае должно быть, так как оно вставлено в контейнер. Независимо от любой предыдущей логики
                                        imposedHash1d.Add(matrix[m].image_id);
                                    }
                                }
                            }
                        }
                    }

                    messages.Add($"fullImposed2d before filtering iteration = {iteration}:");
                    messages.Add($"2: finishsorted.Count = {fullImposed2d.Count()}");

                    foreach (var item in fullImposed2d)
                    {
                        messages.Add($"- Name: {item.Templ.Name}; Size = {item.Templ.Frames.FramesCount}");
                        messages.Add($"- Segments ({item.Segments.Count}):");

                        foreach (var s in item.Segments)
                        {
                            messages.Add($" - KeyId = {s.KeyId}, Index = {s.Index}");
                        }
                        messages.Add($"- TreeNodeItems ({item.Count()}):");

                        foreach (var id in item)
                        {
                            var data = InputData[id.FileId];
                            messages.Add($" - File: {Path.Combine(data.FilePath, data.FileName)}; OrderBy = {data.OrderBy}");
                        }
                    }
                    messages.Add("");

                    // Сортируем и отсеиваем лишнее (повторяющиеся ссылки на изображения) в разворотах, собранных из разных сегментов
                    // Принцип - сохраним наибольшее количество изображений
                    List<Link<ProjectContainer>> dublesList = new List<Link<ProjectContainer>>();

                    for (int j = 0; j < fullImposed2d.Count; j++)
                    {
                        Link<ProjectContainer> last = null;

                        for (int k = 0; k < dublesList.Count; k++)
                        {
                            if ((last = dublesList[k].Find(fullImposed2d[j], (x, y) => x == y)) != null)
                            {
                                break;
                            }
                        }

                        if (last == null)
                        {
                            last = new Link<ProjectContainer>(fullImposed2d[j]);
                            dublesList.Add(last);
                        }

                        for (int h = j + 1; h < fullImposed2d.Count; h++)
                        {
                            var intersect = fullImposed2d[j].Select(x => x.FileId).Intersect(fullImposed2d[h].Select(y => y.FileId));

                            if (intersect.Count() > 0)
                            {
                                if (last.Next != null) throw new InvalidOperationException();
                                last.Next = new Link<ProjectContainer>(fullImposed2d[h]);
                            }
                        }
                    }

                    // Разбиваем связанные списки пересечений на единицные узлы в две кучи по принципу - четные и нечетный чтобы убрать пересечения. В какой куче окажется больше файлов, ту и используем
                    // Перезаписываем данные fullImposed2d
                    fullImposed2d.Clear();

                    for (int k = 0; k < dublesList.Count; k++)
                    {
                        List<ProjectContainer> odd = new List<ProjectContainer>();
                        List<ProjectContainer> even = new List<ProjectContainer>();

                        int h = 1;
                        int count = dublesList[k].Count();
                        ProjectContainer clearthishash = null;

                        foreach (var linkitem in dublesList[k].GetItems())
                        {
                            if (h.IsOdd())
                            {
                                if (h == count && linkitem.Next == dublesList[k])
                                {
                                    // Если последний элемент нечетный, то нельзя чтобы он оказался зациклен на первый (тоже нечетный). Иначе останется пересечение между двумя нечетными
                                    // Нужно выбрать один из них
                                    if (linkitem != dublesList[k] && linkitem.Value.Templ.Frames.FramesCount > dublesList[k].Value.Templ.Frames.FramesCount)
                                    {
                                        odd.Remove(dublesList[k].Value);
                                        odd.Add(linkitem.Value);
                                        clearthishash = dublesList[k].Value;
                                    }
                                    else
                                    {
                                        // Ничего не делаем, оставляем первый. Только почисть за ненужным хэш
                                        if (linkitem != dublesList[k]) clearthishash = linkitem.Value;
                                    }
                                }
                                else
                                {
                                    odd.Add(linkitem.Value);
                                }
                            }
                            else
                            {
                                even.Add(linkitem.Value);
                            }

                            h++;
                        }

                        List<ProjectContainer> maxHeap = null;
                        List<ProjectContainer> minHeap = null;

                        if (odd.Sum(x => x.Templ.Frames.FramesCount) > even.Sum(x => x.Templ.Frames.FramesCount))
                        {
                            maxHeap = odd;
                            minHeap = even;
                        }
                        else
                        {
                            maxHeap = even;
                            minHeap = odd;
                        }

                        foreach (var max in maxHeap)
                        {
                            fullImposed2d.Add(max);
                        }

                        // Отсеялось некоторое количество собранных файлов. Нужно убрать ссылки на них из хеша
                        if (clearthishash != null)
                        {
                            minHeap.Add(clearthishash);
                        }

                        foreach (var rem in minHeap)
                        {
                            foreach (var id in rem)
                            {
                                if (!maxHeap.Exists(x => x.Any(y => y.FileId == id.FileId)) && imposedHash2d.Contains(id.FileId))
                                {
                                    imposedHash2d.Remove(id.FileId);
                                    messages.Add($"imposedHash2d removed: id = {id.FileId}: File = {InputData[id.FileId].FileName}");
                                }
                            }
                        }
                    }

                    messages.Add($"fullImposed2d after filtering iteration = {iteration}:");
                    messages.Add($"2: finishsorted.Count = {fullImposed2d.Count()}");

                    foreach (var item in fullImposed2d)
                    {
                        messages.Add($"- Name: {item.Templ.Name}; Size = {item.Templ.Frames.FramesCount}");
                        messages.Add($"- Segments ({item.Segments.Count}):");

                        foreach (var s in item.Segments)
                        {
                            messages.Add($" - KeyId = {s.KeyId}, Index = {s.Index}");
                        }
                        messages.Add($"- TreeNodeItems ({item.Count()}):");

                        foreach (var id in item)
                        {
                            var data = InputData[id.FileId];
                            messages.Add($" - File: {Path.Combine(data.FilePath, data.FileName)}; OrderBy = {data.OrderBy}");
                        }
                    }
                    messages.Add("");

                    var finishsorted1 = fullImposed1d.Concat(fullImposed2d).OrderBy(x => MathEx.Avg(x.Select(y => InputData[y.FileId].OrderBy)));
                    messages.Add("Finish merged 1:");
                    messages.Add($"2: finishsorted.Count = {finishsorted1.Count()}");

                    foreach (var item in finishsorted1)
                    {
                        messages.Add($"- Name: {item.Templ.Name}; Size = {item.Templ.Frames.FramesCount}");
                        messages.Add($"- Segments ({item.Segments.Count}):");

                        foreach (var s in item.Segments)
                        {
                            messages.Add($" - KeyId = {s.KeyId}, Index = {s.Index}");
                        }
                        messages.Add($"- TreeNodeItems ({item.Count()}):");

                        foreach (var id in item)
                        {
                            var data = InputData[id.FileId];
                            messages.Add($" - File: {Path.Combine(data.FilePath, data.FileName)}; OrderBy = {data.OrderBy}");
                        }
                    }
                    messages.Add("");

                    if (iteration == 0)
                    {
                        files = files.Where(f => !imposedHash2d.Contains(f.Index));
                        k2 = 0.4f;
                        // В хэше все обработанные файлы. Оставляем только которые собрались в шаблоны, неполностью собранные удаляем
                        imposedHash1d.RemoveWhere(x => !imposedHash2d.Contains(x));

                        centrouds1d.Clear();
                        centrouds2d.Clear();

                        iteration++;

                        messages.Add($" - Step 1 k2 = {k2}; Init file count: imposedHash1d = {imposedHash1d.Count}; imposedHash2d = {imposedHash2d.Count}; next file = {files.Count()}");

                        Log.WriteInfo("Сборка страниц", messages.ToArray());
                        messages.Clear();

                        continue;
                    }
                    else
                    {
                        if (minFramesInTemplate == 0)
                        {
                            step_1 = new List<ProjectContainer>(fullImposed1d.Concat(fullImposed2d).OrderBy(x => MathEx.Avg(x.Select(y => InputData[y.FileId].OrderBy))));

                            minFramesInTemplate++;
                            iteration = 0;
                            k2 = 0.8f;

                            files = InputData;
                            centrouds1d.Clear();
                            centrouds2d.Clear();
                            imposedHash1d.Clear();
                            imposedHash2d.Clear();
                            fullImposed1d.Clear();
                            fullImposed2d.Clear();

                            continue;
                        }
                        else
                        {
                            step_2 = new List<ProjectContainer>(fullImposed1d.Concat(fullImposed2d).OrderBy(x => MathEx.Avg(x.Select(y => InputData[y.FileId].OrderBy))));
                            break;
                        }
                    }
                }

                Log.WriteInfo("Сборка страниц", messages.ToArray());
                messages.Clear();

                PdfCombainer(step_1, @"c:\Temp\step_1.pdf");
                PdfCombainer(step_2, @"c:\Temp\step_2.pdf");

                return;

           
                Segment dateSegment = topSegment[SegmentTypeEnum.DateCreate];
                Segment gpsSegment = topSegment[SegmentTypeEnum.Geolocation];

                StreamLogger.WriteLine($"");
                StreamLogger.WriteLine($"Data Create:");
                for (int i = 0; i < topSegment[SegmentTypeEnum.DateCreate].TreeNodeItems.Count; i++)
                {
                    StreamLogger.WriteLine($" Data Create KeyId = {topSegment[SegmentTypeEnum.DateCreate].TreeNodeItems[i].KeyId}; Collection type = {topSegment[SegmentTypeEnum.DateCreate].TreeNodeItems[i].Data.GetType().ToString()}");
                    foreach (int pos in topSegment[SegmentTypeEnum.DateCreate].TreeNodeItems[i].Data)
                    {
                        StreamLogger.WriteLine($"  File index = {pos}");
                    }
                }

                StreamLogger.WriteLine($"");
                StreamLogger.WriteLine($"Geolocation:");
                for (int i = 0; i < topSegment[SegmentTypeEnum.Geolocation].TreeNodeItems.Count; i++)
                {
                    StreamLogger.WriteLine($" Geolocation Group KeyId = {topSegment[SegmentTypeEnum.Geolocation].TreeNodeItems[i].KeyId}; Collection type = {topSegment[SegmentTypeEnum.Geolocation].TreeNodeItems[i].Data.GetType().ToString()}");
                    foreach (int pos in topSegment[SegmentTypeEnum.Geolocation].TreeNodeItems[i].Data)
                    {
                        StreamLogger.WriteLine($"  File index = {pos}");
                    }
                }

                StreamLogger.WriteLine($"");
                StreamLogger.WriteLine($"Size:");
                for (int i = 0; i < topSegment[SegmentTypeEnum.Size].TreeNodeItems.Count; i++)
                {
                    StreamLogger.WriteLine($" Size Group KeyId = {topSegment[SegmentTypeEnum.Size].TreeNodeItems[i].KeyId}; Collection type = {topSegment[SegmentTypeEnum.Size].TreeNodeItems[i].Data.GetType().ToString()}");
                    foreach (int pos in topSegment[SegmentTypeEnum.Size].TreeNodeItems[i].Data)
                    {
                        StreamLogger.WriteLine($"  File index = {pos}; Size = {((float)InputData[pos].ImageSize.Width * InputData[pos].ImageSize.Height)}");
                    }
                }
              
                StreamLogger.WriteLine($"");
                StreamLogger.WriteLine($"Ratio:");
                for (int i = 0; i < topSegment[SegmentTypeEnum.Ratio].TreeNodeItems.Count; i++)
                {
                    StreamLogger.WriteLine($" Ratio Group KeyId = {topSegment[SegmentTypeEnum.Ratio].TreeNodeItems[i].KeyId}; Collection type = {topSegment[SegmentTypeEnum.Ratio].TreeNodeItems[i].Data.GetType().ToString()}");
                    foreach (int pos in topSegment[SegmentTypeEnum.Ratio].TreeNodeItems[i].Data)
                    {
                        StreamLogger.WriteLine($"  File index = {pos}; Ratio = {(float)InputData[pos].ImageSize.Width / (float)InputData[pos].ImageSize.Height}");
                    }
                }
            
            }
        }

        private List<KeyValuePair<ValueTuple<Segment, Segment>, Dictionary<Segment, List<int>>>> GetSameDateAndGeoGroupsInFolder(Segment _segment, Segment _dateSegment, Segment _gpsSegment)
        {
            List<KeyValuePair<ValueTuple<Segment, Segment>, Dictionary<Segment, List<int>>>> result = new List<KeyValuePair<ValueTuple<Segment, Segment>, Dictionary<Segment, List<int>>>>();

            for (int j = 0; j < _segment.TreeNodeItems.Count; j++)
            {
                foreach (int pos in _segment.TreeNodeItems[j].Data)
                {
                    Segment finddgps = null;
                    foreach (var datesegment in _gpsSegment.TreeNodeItems)
                    {
                        if (datesegment.Data.Contains(pos))
                        {
                            finddgps = datesegment; break;
                        }
                    }
                    if (finddgps == null) break;

                    Segment finddate = null;
                    foreach (var datesegment in _dateSegment.TreeNodeItems)
                    {
                        if (datesegment.Data.Contains(pos))
                        {
                            finddate = datesegment; break;
                        }
                    }

                    if (finddate != null)
                    {
                        List<int> addHash = null;
                        for (int h = 0; h < result.Count; h++)
                        {
                            if (result[h].Key.Item1 == finddgps && result[h].Key.Item2 == finddate)
                            {
                                if (!result[h].Value.TryGetValue(_segment.TreeNodeItems[j], out addHash))
                                {
                                    result[h].Value.Add(_segment.TreeNodeItems[j], addHash = new List<int>());
                                }
                                break;
                            }
                        }
                        if (addHash == null)
                        {
                            Dictionary<Segment, List<int>> dict = new Dictionary<Segment, List<int>>();
                            result.Add(new KeyValuePair<ValueTuple<Segment, Segment>, Dictionary<Segment, List<int>>>(new ValueTuple<Segment, Segment>(finddgps, finddate), dict));
                            dict.Add(_segment.TreeNodeItems[j], addHash = new List<int>());
                        }
                        addHash.Add(pos);
                    }
                }
            }
            return result;
        }

        public void AllJpgToPdf(string _filename)
        {
            AllJpgToPdf(this, InputData, _filename);
        }

        public int[,] ProcessData()
        {
            return Process(this, InputData);
        }
        static int[,] Process(Project _project, List<ExifTaggedFile> _data)
        {
            int[,] matrix = null;

            for (int i = 0; i < _data.Count; i++)
            {
                string dirName = _data[i].Key;
                //var files = _data[i].Value.Select(x => x.FileName).GetGroups();
                var files = _data[i].Value;
                int fileCount = files.Sum(x => x.Value.Count);
                matrix = new int[fileCount, fileCount];
                int counter = 0;
                // Ключи файловых групп в отсортированном порядке
                var keys = new List<string>(files.Keys.OrderBy(x => x));

                for (int keyid = 0; keyid < keys.Count; keyid++)
                {
                    var key = keys[keyid];
             
                    if (key != "*")
                    {
                        var segment = files[key];
                        // Связка между логическими группами. Имена уже непохожи, но находятся в одной папке
                        // Содержание может быть независимым
                        if (keyid > 0)
                        {
                            int groupcounter = 0;
                            for (int linkgroupid = 0; linkgroupid < keyid; linkgroupid++)
                            {
                                var linkkey = keys[linkgroupid];
                                if (linkkey != "*")
                                {
                                    // Связываем текущую группу со всеми предыдущими группами в одной папке, не только с предыдущей по порядку
                                    groupcounter = groupcounter + files[linkkey].Count;
                                    // Индекс ставим небольшой
                                    matrix[groupcounter - 1, counter] = 20;
                                    matrix[counter, groupcounter - 1] = 20;
                                }
                            }
                        }

                        for (int j = 0; j < segment.Count; j++)
                        {
                            // Логически связанные имена файлов.
                            for (int k = 0; k < segment.Count; k++)
                            {
                                // Индекс должен быть достаточно большой.
                                // Наверняка в логически схожих именах должно быть зависимое содержание. 
                                if (Math.Abs(j - k) == 1)
                                {
                                    // Пусть будет пока 100 из 100 максимальных
                                    matrix[counter + k, counter + j] = 100;
                                }
                                else
                                {
                                    if (j != k)
                                    {
                                        // Файлы в логической группе файлов, но не по порядку. Содержание также должно быть схожим. Немного уменьшаем индекс
                                        matrix[counter + k, counter + j] = 80;
                                    }
                                }
                            }
                        }
                        counter = counter + segment.Count;
                    }
                }

                if (files.ContainsKey("*"))
                {
                    var ungrouped = files["*"];
                    // Группа без внятной норядковой нумерации файлов. Просто сортируем по строковому признаку
                    // Далее ориентируемся в основном на дату создания и геоданные. Это будет основной ключ группировки
                    if (ungrouped.Count > 0)
                    {
                        // Соединиим пока просто с предыдущими
                        if (counter > 0)
                        {
                            for (int keyid = 0; keyid < keys.Count; keyid++)
                            {
                                var key = keys[keyid];
                                if (key != "*")
                                {
                                    int groupcounter = 0;
                                    // Связываем текущую группу со всеми предыдущими группами в одной папке, не только с предыдущей по порядку
                                    groupcounter = groupcounter + files[key].Count;
                                    // Индекс ставим небольшой
                                    matrix[groupcounter - 1, counter] = 25;
                                    matrix[counter, groupcounter - 1] = 25;
                                }
                            }
                        }
                        StreamLogger.WriteLine("1:");
                        foreach (var file1 in ungrouped)
                        {
                            StreamLogger.WriteLine(file1.FileName);
                        }
                        ungrouped.Sort((x, y) => String.Compare(x.FileName, y.FileName, true));
                        StreamLogger.WriteLine("2:");
                        foreach (var file2 in ungrouped)
                        {
                            StreamLogger.WriteLine(file2.FileName);
                        }
                        for (int j = 0; j < ungrouped.Count; j++)
                        {
                            // Неявно логически связанные имена файлов.
                            for (int k = 0; k < ungrouped.Count; k++)
                            {
                                // Индекс будем динамически менять в интервале, например от 5 до 100
                                // В зависимости от близости даты создания и геопозиции съемки
                                if (Math.Abs(j - k) == 1)
                                {
                                    // Пока просто рядом стоящим поставим 50
                                    matrix[counter + k, counter + j] = 50;
                                }
                                else
                                {
                                    if (j != k)
                                    {
                                        // А не рядом стоящим 40
                                        matrix[counter + k, counter + j] = 40;
                                    }
                                }
                            }
                        }
                        counter = counter + ungrouped.Count;
                    }
                }
                break;
            }
            return matrix;
        }

        static void AllJpgToPdf(Project _project, List<ExifTaggedFile> _data, string _filename)
        {
            int templateIndex = 0;
            int templateSide = 0;
            int pageId = 0;

            GdPicturePDF doc = new GdPicturePDF();

            doc.NewPDF(PdfConformance.PDF1_7);
            doc.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
            doc.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
            doc.SetJpegQuality(100);
            doc.EnableCompression(true);
            doc.SetOrigin(PdfOrigin.PdfOriginBottomLeft);

            using (GdPictureImaging oImage = new GdPictureImaging())
            {

                for (int j = 0; j < _data.Count; j++)
                {
                    if (_data[j].ImageType == TagFileTypeEnum.JPEG)
                    {
                        
                        Template template = null;
                        int counter = 0;
                        int currentTemplate = templateIndex;
                        int currentSide = 0;
                        try
                        { 
                        while (counter < _project.Product.TemplateKeys.Count)
                        {
                            template = _project.Product.TemplateKeys[currentTemplate];
                            if (template.Frames.Navigator.MoveNext())
                            {
                                currentSide = template.Frames.Navigator.Side;
                                break;
                            }
                            else
                            {
                                    try
                                    {
                                        template.Frames.Navigator.Clear();

                                        if (currentTemplate + 1 < _project.Product.TemplateKeys.Count)
                                        {
                                            currentTemplate++;
                                        }
                                        else
                                        {
                                            currentTemplate = 0;
                                        }
                                    }
                                    catch (Exception ex) 
                                    {
                                        StreamLogger.WriteLine("**");
                                        throw;
                                    }
                                counter++;
                            }
                        }
                        }
                        catch (Exception ex) 
                        {
                            StreamLogger.WriteLine(ex.Message);
                            StreamLogger.WriteLine($"template = null: {(template == null).ToString()}");
                            if (template != null)
                            {
                                StreamLogger.WriteLine($"template = {template.Name}");
                            }
                            StreamLogger.WriteLine($" index = {j}; name = {Path.Combine(_data[j].FilePath,_data[j].FileName)}");
                            break;
                        }

                        if (template != null && template.Frames.Navigator.Current != default)
                        {
                            float xShift = currentSide == 0 ? 0 : _project.Product.Trim.Width;

                            if (doc.GetPageCount() == 0 || templateIndex != currentTemplate || templateSide != currentSide)
                            {
                                doc.NewPage(_project.Product.Trim.Width, _project.Product.Trim.Height);
                                doc.SelectPage(++pageId);
                            }
                            //
                            var rect = template.Frames.Navigator.Current;

                            _project.FileToFrame(doc, oImage, rect, xShift, Path.Combine(_data[j].FilePath, _data[j].FileName));

                            templateSide = currentSide;
                            templateIndex = currentTemplate;
                        }
                    }
                }

            }

            doc.SaveToFile(_filename);
            doc.CloseDocument();
            doc.Dispose();
        }
        */
        /*
        private void PdfCombainer(IEnumerable<ProjectContainer> _data, string _filename)
        {
            if (_data == null || _data.Count() == 0) return;

            GdPicturePDF doc = new GdPicturePDF();

            doc.NewPDF(PdfConformance.PDF1_7);
            doc.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
            doc.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
            doc.SetJpegQuality(100);
            doc.EnableCompression(true);
            doc.SetOrigin(PdfOrigin.PdfOriginBottomLeft);

            int pageId = 0;
            var extraframes = new List<ValueTuple<Fill, string, Point>>();

            using (GdPictureImaging oImage = new GdPictureImaging())
            {
                foreach (var item in _data)
                {
                    Template template = item.Templ;

                    for (int i = 0; i < item.TreeNodeItems.Count; i++)
                    {
                        var side = item.TreeNodeItems[i];
                        bool pageadded = false;
                        extraframes.Clear();

                        for (int k = 0; k < side.GetLength(0); k++)
                        {
                            for (int m = 0; m < side.GetLength(1); m++)
                            {
                                if (item.TreeNodeItems[i][k, m].FileId == -1) continue;

                                if (!pageadded)
                                {
                                    doc.NewPage(Product.Trim.Width, Product.Trim.Height);
                                    doc.SelectPage(++pageId);
                                    pageadded = true;
                                }

                                int xShift = i == 0 ? 0 : Product.Trim.Width;
                                //string filename = Path.Combine(InputData[item.TreeNodeItems[i][k, m].FileId].FilePath, InputData[item.TreeNodeItems[i][k, m].FileId].FileName);
                                string filename = Path.Combine(ProjectPath, "~Files", InputData[item.TreeNodeItems[i][k, m].FileId].GUID + ".jpg");
                                var rect = template.Frames[i][k, m].ToRectangle();

                                if (i == 0 && rect.X + rect.Width > Product.Trim.Width)
                                {
                                    extraframes.Add(new ValueTuple<Fill, string, Point>(rect, filename, item.TreeNodeItems[i][k, m].Shift));
                                }

                                FileToFrame(doc, oImage, rect, xShift, filename, item.TreeNodeItems[i][k, m].Shift);
                            }
                        }

                        if (extraframes.Count > 0)
                        {
                            doc.NewPage(Product.Trim.Width, Product.Trim.Height);
                            doc.SelectPage(++pageId);

                            foreach (var frame in extraframes) 
                            {
                                FileToFrame(doc, oImage, frame.Item1, Product.Trim.Width, frame.Item2, frame.Item3);
                            }
                        }
                    }
                }
            }

            doc.SaveToFile(_filename);
            doc.CloseDocument();
            doc.Dispose();
        }
        */
        private float FileToFrame(GdPicturePDF _doc, GdPictureImaging _obj, Rectangle _frame, int _shift, string _file, Point _correction)
        {
            int id = _obj.CreateGdPictureImageFromFile(_file);
            int iWidth = _obj.GetWidth(id);
            int iHeight = _obj.GetHeight(id);
            string imagename = _doc.AddImageFromGdPictureImage(id, false, false);
            float effectiveRes = 0f;

            _doc.SaveGraphicsState();
            try
            {
                GdPictureStatus res;

                PointF[] points = new PointF[4] { new PointF(_frame.X - _shift, _frame.Y), new PointF(_frame.X - _shift, _frame.Y + _frame.Height), new PointF(_frame.X - _shift + _frame.Width, _frame.Y + _frame.Height), new PointF(_frame.X - _shift + _frame.Width, _frame.Y) };
                _doc.AddGraphicsToPath(new GraphicsPath(points, new byte[4] { (byte)(PathPointType.Start | PathPointType.Line), (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line }));
                _doc.ClipPath();

                var fitedRect = _frame.FitToFrame(iWidth, iHeight, _shift);
                res = _doc.DrawImage(imagename, fitedRect.Item1.X + _correction.X, fitedRect.Item1.Y + _correction.Y, fitedRect.Item1.Width, fitedRect.Item1.Height);
                effectiveRes = fitedRect.Item2;
                
                if (res != GdPictureStatus.OK)
                {
                    Log.WriteError("FileToFrame", $"Ошибка отрисовки изображения во фрейм. Status = {res}");
                }
            }
            catch (Exception ex)
            {
                Log.WriteError("FileToFrame", $"Исключение при отрисовке изображения во фрейм '{ex.Message}'");
            }

            _doc.RestoreGraphicsState();
            _obj.ReleaseGdPictureImage(id);

            return effectiveRes;
        }
        protected void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (mIsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
                Log.Close();
            }

            mIsDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~Project()
        {
            Dispose(false);
        }
    }
}
