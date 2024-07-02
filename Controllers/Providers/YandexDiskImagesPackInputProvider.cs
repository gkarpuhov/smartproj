using Newtonsoft.Json.Linq;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Smartproj
{
    public class YandexDiskImagesPackInputProvider : AbstractInputProvider
    {
        private bool mIsLocked;
        public YandexDiskImagesPackInputProvider() : base()
        {
            mIsLocked = false;
        }
        bool CheckAndBlock()
        {
            lock (mSyncRoot)
            {
                if (!mIsLocked)
                {
                    mIsLocked = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        void UnBlock()
        {
            lock (mSyncRoot)
            {
                mIsLocked = false;
            }
        }
        protected override void ProcessHandler(object _obj)
        {
            Project project = Owner.Project;
            IAdapter adapter = project.Owner?.Owner?.Adapters.Find(x => x.UID == AdapterId);

            if (adapter == null) 
            {
                Log?.WriteError("YandexDiskImagesPackInputProvider.ProcessHandler", $"{project.ProjectId} => Не определен адаптер входных данных");
                return;
            }
            Job job = null;
            bool nextprocess = false;

            if (Enabled && CheckAndBlock())
            {
                try
                {
                    nextprocess = adapter.GetNext(project, this, out job);
                }
                finally
                {
                    UnBlock();
                }
            }
            if (!nextprocess) return;

            List<KeyValuePair<string, List<string>>> extractData = new List<KeyValuePair<string, List<string>>>();
            FileProcess.ExtractFiles(Path.Combine(job.JobPath, "~Original"), extractData, adapter.FileDataFilter, true, true);

            ExifTaggedFileSegments topSegment = (ExifTaggedFileSegments)job.ProcessingSpace.Clusters;
            ExifTaggedFileSegments fsSegment = (ExifTaggedFileSegments)topSegment.Add(SegmentTypeEnum.FileStructure, "0");

            int count = extractData.Sum(x => x.Value.Count);

            Log?.WriteInfo("HotFolderImagesInputProvider.ProcessHandler", $"{project.ProjectId} => Файловая структура для выполнения процесса '{job.UID}' загружена: {count} файлов");

            int index = 0;

            for (int i = 0; i < extractData.Count; i++)
            {
                List<ExifTaggedFile> parsed = null;
                if (AutoExifParse)
                {
                    parsed = ExifParser(extractData[i].Key, extractData[i].Value, index, adapter.FileDataFilter);
                    job.InputDataContainer.AddRange(parsed);
                }
                else
                {
                    parsed = new List<ExifTaggedFile>();
                    for (int j = 0; j < extractData[i].Value.Count; j++)
                    {
                        ExifTaggedFile item = new ExifTaggedFile(index, extractData[i].Value[j], extractData[i].Key);
                        parsed.Add(item);
                        job.InputDataContainer.Add(item);
                    }
                }

                index = index + parsed.Count;

                if (parsed.Count > 0)
                {
                    fsSegment.ImportFiles(extractData[i].Key, parsed, x => Path.GetFileNameWithoutExtension(x.FileName), x => x.Index);
                }
            }

            if (job.InputDataContainer.Count > 0)
            {
                // В результате формирования сегментов файловой структуры (ImportFiles), порядок сортировки файлов может быть изменен
                // Свойство Index уже не будет отражать порядок расположения файлов, теперь за это будет отвечать свойство OrderBy
                // Установим сквозной порядковый индекс по возрастанию в соответствии со структурой сегментов: 

                int ordercounter = 0;
                for (int i = 0; i < fsSegment.ChildNodes.Count; i++)
                {
                    var directory = fsSegment.ChildNodes[i];
                    for (int j = 0; j < directory.ChildNodes.Count; j++)
                    {
                        List<int> d = (List<int>)directory.ChildNodes[j].Data;
                        for (int k = 0; k < d.Count; k++)
                        {
                            job.InputDataContainer[d[k]].OrderBy = ordercounter++;
                        }
                        // Меняем список на хеш-контейнер для более быстрой обработки
                        //directory.ChildNodes[j].Data = new HashSet<int>(directory.ChildNodes[j].Data);
                    }
                }

                Log?.WriteInfo("HotFolderImagesInputProvider.ProcessHandler", $"{project.ProjectId} => Процесс формирование данных для обработки завершен. Процесс '{job.UID}'");

                job.Status = ProcessStatusEnum.Processing;
                
                if (job.Product.Controllers != null)
                {
                    foreach (AbstractController controller in job.Product.Controllers.OrderByDescending(x => x.Priority))
                    {
                        if (job.Status != ProcessStatusEnum.Processing)
                        {
                            Log?.WriteInfo("HotFolderImagesInputProvider.ProcessHandler", $"{Owner.Project.ProjectId} => Процесс прерван '{job.UID}'");
                            break;
                        }
                        DateTime start = DateTime.Now;

                        controller.Start(new object[] { job });

                        Log?.WriteInfo($"{controller.GetType().Name}", $"{Owner?.Project?.ProjectId}: Завершено. Общее время обработки = {Math.Round((DateTime.Now - start).TotalSeconds)} сек");
                    }
                }
                if (DefaultOutput != null)
                {
                    foreach (AbstractController controller in DefaultOutput.OrderByDescending(x => x.Priority))
                    {
                        if (job.Status != ProcessStatusEnum.Processing)
                        {
                            Log?.WriteInfo("HotFolderImagesInputProvider.ProcessHandler", $"{Owner.Project.ProjectId} => Процесс прерван '{job.UID}'");
                            break;
                        }
                        controller.Start(new object[] { job });
                    }
                }

                if (job.Status == ProcessStatusEnum.Processing)
                {
                    job.Status = ProcessStatusEnum.Finished;
                    Log?.WriteInfo("HotFolderImagesInputProvider.ProcessHandler", $"{Owner.Project.ProjectId} => Выполнение процесса '{job.UID}' успешно завершено");
                }
                else
                {
                    Log?.WriteError("HotFolderImagesInputProvider.ProcessHandler", $"{Owner.Project.ProjectId} => Выполнение процесса '{job.UID}' не выполнено");
                }
            }

            job.Dispose();
        }
        private List<ExifTaggedFile> ExifParser(string _dirNameKey, List<string> _files, int _firstIndex, TagFileTypeEnum _filter)
        {
            List<ExifTaggedFile> data = new List<ExifTaggedFile>();
            var extractor = new ExifTool(new ExifToolOptions() { EscapeTagValues = false, ExtractICCProfile = true });
            int index = _firstIndex;
            var list = extractor.ExtractAllAsync(_dirNameKey);
            list.Wait();

            for (int j = 0; j < _files.Count; j++)
            {
                IEnumerable<KeyValuePair<string, IEnumerable<JProperty>>> tags = null;
                string key = Path.Combine(_dirNameKey, _files[j]).Replace("\\", "/");

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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                    }
                    // ColorComponents
                    currentGroupKey = "ColorComponents";
                    findValue = FindValue(tags, currentGroupKey, false, groupsOrder);
                    if (findValue.Item3 != "" && byte.TryParse(findValue.Item1, out colorComponents))
                    {
                    }
                    else
                    {
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                                //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                            }
                        }
                    }
                    else
                    {
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
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
                                //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                            }
                        }
                    }
                    else
                    {
                        //StreamLogger.WriteLine($"!!! {filesTree[i].Value[j]}: Не определено значение '{currentGroupKey}'");
                    }

                    TagFileTypeEnum fileNameType = _files[j].ToFileType();
                    TagMIMETypeEnum fileMimeType = mIMEType.ToMIMEType();

                    if (fileNameType != TagFileTypeEnum.UNDEFINED)
                    {
                        if (fileNameType.ToString() != fileMimeType.ToString() && fileNameType != TagFileTypeEnum.JFIF)
                        {
                            //errors.Add($"{filesTree[i].Value[j]}: Не определен формат файла");
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

                    if ((_filter & fileNameType) != fileNameType)
                    {
                        //errors.Add($"{filesTree[i].Value[j]}: Не определен формат файла");
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
                                //errors.Add($"{filesTree[i].Value[j]}: Не определен формат цветового пространства");
                                continue;
                        }
                    }

                    if (bitsPerSample != null && imagePixelDepth != null && bitsPerSample.Length != 0 && imagePixelDepth.Length != 0)
                    {
                        if (bitsPerSample[0] != imagePixelDepth[0])
                        {
                            //errors.Add($"{filesTree[i].Value[j]}: Ошибка в значении BitsPerSample или ImagePixelDepth");
                            continue;
                        }
                    }

                    if (bitsPerSample != null && bitsPerSample.Length > 0) bitsPerSampleValue = bitsPerSample[0];
                    if (imagePixelDepth != null && imagePixelDepth.Length > 0) bitsPerSampleValue = imagePixelDepth[0];


                    if (bitsPerSampleValue == 0)
                    {
                        //errors.Add($"{filesTree[i].Value[j]}: Не определено значение 'BitsPerSample'");
                        continue;
                    }

                    //if (_minPixelsCount < imageSize.Width * imageSize.Height)
                    {
                        ExifTaggedFile newitem = new ExifTaggedFile(index++, _files[j], _dirNameKey, imageSize, fileCreateDate, fileNameType, tags, colorMode, hasGps, gps, colorProfile, bitsPerSampleValue, false, hasAlpha, false);
                        newitem.AddStatus(ImageStatusEnum.ExifData);
                        data.Add(newitem);
                    }
                    //else
                    {
                        //warnings.Add($"Недостаточный размер изображения {imageSize.Width}x{imageSize.Height}. Файл '{filesTree[i].Value[j]}' будет проигнорирован");
                    }
                }
            }

            return data;
        }
    }
}
