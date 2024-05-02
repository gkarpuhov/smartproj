using Emgu.CV;
using lcmsNET;
using Smartproj.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Encoder = System.Drawing.Imaging.Encoder;

namespace Smartproj
{


    public partial class Project
    {
        /*
private int ItemNextTo(Segment _item1, Segment _item2)
{
    // _item1, _item2 - сегмент данных
    if (_item1 != _item2)
    {
        if (_item1.Parent.Parent.Index - _item2.Parent.Parent.Index == 1)
        {
            if (_item1.Parent.Index == 0 && _item2.Parent.Index == _item2.Parent.Parent.Degree - 1)
            {
                // _item1 > _item2
                return 1;
            }
        }
        if (_item1.Parent.Parent.Index - _item2.Parent.Parent.Index == -1)
        {
            if (_item2.Parent.Index == 0 && _item1.Parent.Index == _item1.Parent.Parent.Degree - 1)
            {
                // _item2 > _item1
                return -1;
            }
        }
        // Сегменты внутри одной папки
        if (_item1.Parent.Parent.Index - _item2.Parent.Parent.Index == 0)
        {
            if (Math.Abs(_item1.Parent.Index - _item2.Parent.Index) == 1)
            {
                return _item1.Parent.Index - _item2.Parent.Index;
            }
        }
    }

    return 0;
}

private void InputNormalizer(IEnumerable<ExifTaggedFile> _inputData)
{

    Dictionary<TagColorModeEnum, Profile> defaultProfiles = new Dictionary<TagColorModeEnum, Profile>
    {
        { TagColorModeEnum.RGB, null },
        { TagColorModeEnum.CMYK, null },
        { TagColorModeEnum.Grayscale, null },
        { TagColorModeEnum.Lab, Profile.CreateLab4(Colorimetric.D50_xyY) }
    };

    List<string> messages = new List<string>();
    List<string> warnings = new List<string>();
    List<string> errors = new List<string>();

    if (File.Exists(Path.Combine(Profiles, DefaultRGBProfile)))
    {
        defaultProfiles[TagColorModeEnum.RGB] = Profile.Open(Path.Combine(Profiles, DefaultRGBProfile), "r");
        var id = defaultProfiles[TagColorModeEnum.RGB].HeaderProfileID;
        string description = defaultProfiles[TagColorModeEnum.RGB].GetProfileInfo(InfoType.Description, "en", "US");
        messages.Add($"{DefaultRGBProfile}: Использован RGB профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.RGB].Version};  '[{String.Join(" ", id)}] [{description}]'");
    }
    if (File.Exists(Path.Combine(Profiles, DefaultCMYKProfile)))
    {
        defaultProfiles[TagColorModeEnum.CMYK] = Profile.Open(Path.Combine(Profiles, DefaultCMYKProfile), "r");
        var id = defaultProfiles[TagColorModeEnum.CMYK].HeaderProfileID;
        string description = defaultProfiles[TagColorModeEnum.CMYK].GetProfileInfo(InfoType.Description, "en", "US");
        messages.Add($"ImageID {DefaultCMYKProfile}: Использован CMYK профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.CMYK].Version};  '[{String.Join(" ", id)}] [{description}]'");
    }
    if (File.Exists(Path.Combine(Profiles, DefaultGRAYProfile)))
    {
        defaultProfiles[TagColorModeEnum.Grayscale] = Profile.Open(Path.Combine(Profiles, DefaultGRAYProfile), "r");
        var id = defaultProfiles[TagColorModeEnum.Grayscale].HeaderProfileID;
        string description = defaultProfiles[TagColorModeEnum.Grayscale].GetProfileInfo(InfoType.Description, "en", "US");
        messages.Add($"ImageID {DefaultGRAYProfile}: Использован Grayscale профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.Grayscale].Version}; '[{String.Join(" ", id)}] [{description}]'");
    }

    ConcurrentQueue<ExifTaggedFile> queue = new ConcurrentQueue<ExifTaggedFile>(_inputData);
    var options = new ProcessThreadOptions() { Queue = queue, DefaultProfiles = defaultProfiles, TempPath = ProjectPath };

    int threadsCount = 4;
    Thread[] pool = new Thread[threadsCount];
    ManualResetEvent[] callback = new ManualResetEvent[threadsCount];
    DateTime start = DateTime.Now;

    for (int i = 0; i < threadsCount; i++)
    {
        pool[i] = new Thread(parallelAction)
        {
            IsBackground = true
        };
        callback[i] = new ManualResetEvent(false);
        pool[i].Start(new ProcessThreadOptions() { Queue = queue, DefaultProfiles = defaultProfiles, TempPath = ProjectPath, ResetEvent = callback[i] });
    }

    WaitHandle.WaitAll(callback);

    messages.Add($"Общее время обработки = {Math.Round((DateTime.Now - start).TotalSeconds)} сек");


    foreach (var profile in defaultProfiles)
    {
        if (profile.Value != null) profile.Value.Dispose();
    }

    for (int i = 0; i < callback.Length; i++)
    {
        callback[i].Dispose();
    }

    Log.WriteAll("Нормализация", messages, warnings, errors);

    void parallelAction(object _object)
    {
        ProcessThreadOptions opt = (ProcessThreadOptions)_object;
        ExifTaggedFile item;
        List<string> m = new List<string>();
        List<string> w = new List<string>();
        List<string> e = new List<string>();

        while (opt.Queue.TryDequeue(out item))
        {
            string tempFiles = Path.Combine(opt.TempPath, "~Files");
            string tempCms = Path.Combine(opt.TempPath, "~Cms");

            var jpegImageCodecInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/jpeg");

            var jpegEncoderParameter = new EncoderParameter(Encoder.Quality, 75L);
            var jpegEncoderParameters = new EncoderParameters(1);
            jpegEncoderParameters.Param[0] = jpegEncoderParameter;

            if (item.ColorSpace != TagColorModeEnum.RGB && item.ColorSpace != TagColorModeEnum.CMYK && item.ColorSpace != TagColorModeEnum.Grayscale)
            {
                item.AddStatus(ImageStatusEnum.NotSupported);
                e.Add($"ImageID {item.Index}: Цветового пространство не поддерживается: '{item.ColorSpace}'. Файл: {item.FileName}");
            }
            if (item.HasTransparency)
            {
                w.Add($"ImageID {item.Index}: Обнаружено наличие прозрачности (будет удалена). Файл: {item.FileName}");
            }
            if (item.Bpc != 8)
            {
                item.AddStatus(ImageStatusEnum.NotSupported);
                e.Add($"ImageID {item.Index}: Требуется формат 8 бит на пиксель: Обнаружено '{item.ColorSpace}:{item.Bpc}'. Задача прервана. Файл: {item.FileName}");
            }

            if (item.HasStatus(ImageStatusEnum.NotSupported)) continue;

            Profile inputProfile = null;
            bool isDefaultProfile = false;

            if (item.HasColorProfile)
            {
                File.WriteAllBytes(Path.Combine(tempCms, item.GUID + ".icc"), item.ColorProfile);

                if ((inputProfile = Profile.Open(Path.Combine(tempCms, item.GUID + ".icc"), "r")) != null)
                {
                    if ((item.ColorSpace == TagColorModeEnum.RGB && inputProfile.ColorSpace != ColorSpaceSignature.RgbData) || (item.ColorSpace == TagColorModeEnum.CMYK && inputProfile.ColorSpace != ColorSpaceSignature.CmykData) || (item.ColorSpace == TagColorModeEnum.Grayscale && inputProfile.ColorSpace != ColorSpaceSignature.GrayData))
                    {
                        item.AddStatus(ImageStatusEnum.Error);
                        e.Add($"ImageID {item.Index}: ОШИБКА - Некорректный формат цветового профиля: Ожидается '{item.ColorSpace}', обнаружен '{inputProfile.ColorSpace}'. Файл: {item.FileName}");
                    }

                    isDefaultProfile = CMS.CompareProfiles(inputProfile, opt.DefaultProfiles[item.ColorSpace]);
                    if (isDefaultProfile)
                    {
                        m.Add($"ImageID {item.Index}: Цветовой профиль источника соответствует профилю по умолчанию. Файл: {item.FileName}");
                    }
                }
                else
                {
                    item.AddStatus(ImageStatusEnum.Error);
                    e.Add($"ImageID {item.Index}: ОШИБКА - Ошибка данных цветового профиля. Файл: {item.FileName}");
                }
            }
            else
            {
                inputProfile = opt.DefaultProfiles[item.ColorSpace];
            }

            if (item.HasStatus(ImageStatusEnum.Error)) continue;


            if (item.ColorSpace == TagColorModeEnum.RGB && !item.HasTransparency && item.Bpc == 8 && (!item.HasColorProfile || isDefaultProfile))
            {
                try
                {
                    if (item.ImageType == TagFileTypeEnum.JPEG)
                    {
                        File.Copy(Path.Combine(item.FilePath, item.FileName), Path.Combine(tempFiles, item.GUID + ".jpg"));
                        item.AddStatus(ImageStatusEnum.OriginalIsReady);
                        m.Add($"ImageID {item.Index}: Не требуется. Файл: {item.FileName} -> {item.GUID + ".jpg"}");
                    }
                    else
                    {
                        using (Bitmap bitmap = new Bitmap(Path.Combine(item.FilePath, item.FileName), false))
                        {
                            if ((bitmap.PixelFormat == PixelFormat.Format24bppRgb && !item.HasTransparency) || (bitmap.PixelFormat == PixelFormat.Format32bppArgb && item.HasTransparency))
                            {
                                bitmap.Save(Path.Combine(tempFiles, item.GUID + ".jpg"), jpegImageCodecInfo, jpegEncoderParameters);
                                item.AddStatus(ImageStatusEnum.FormatTransformed);
                                m.Add($"ImageID {item.Index}: Формат изменен: {item.ImageType.ToString()} -> {"JPEG"}. Файл: {item.FileName} -> {item.GUID + ".jpg"}");
                            }
                            else
                            {
                                item.AddStatus(ImageStatusEnum.Error);
                                e.Add($"ImageID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства: Ожидается '{"Format24bppRgb или PixelFormat.Format32bppArgb"}'. Обнаружен '{bitmap.PixelFormat}' (HasTransparency = {item.HasTransparency}). Файл: {item.FileName}");
                            }
                        }
                    }

                    continue;
                }
                catch (Exception ex)
                {
                    item.AddStatus(ImageStatusEnum.Error);
                    e.Add($"ImageID {item.Index}: ИСКЛЮЧЕНИЕ #1 '{ex.Message}'. Файл: {item.FileName}");
                    continue;
                }
            }

            try
            {
                using (Bitmap bitmap = new Bitmap(Path.Combine(item.FilePath, item.FileName), false))
                {
                    switch (item.ColorSpace)
                    {
                        case TagColorModeEnum.RGB:
                            if (bitmap.PixelFormat != PixelFormat.Format24bppRgb && bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                            {
                                item.AddStatus(ImageStatusEnum.Error);
                                e.Add($"ImageID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства RGB: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                            }
                            break;
                        case TagColorModeEnum.CMYK:
                            if ((int)bitmap.PixelFormat != 0x200F)
                            {
                                item.AddStatus(ImageStatusEnum.Error);
                                e.Add($"ImageID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства CMYK: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                            }
                            break;
                        case TagColorModeEnum.Grayscale:
                            if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                            {
                                item.AddStatus(ImageStatusEnum.Error);
                                e.Add($"ImageID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства GRAY: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                            }
                            break;
                    }

                    if (item.HasStatus(ImageStatusEnum.Error)) continue;

                    if (inputProfile != null && opt.DefaultProfiles[TagColorModeEnum.RGB] != null)
                    {
                        var id = inputProfile.HeaderProfileID;
                        string description = inputProfile.GetProfileInfo(InfoType.Description, "en", "US");

                        m.Add($"ImageID {item.Index}: Использован входной профиль {ColorSpaceSignature.RgbData}: Ver = {inputProfile.Version}; '[{String.Join(" ", id)}] [{description}]'. Файл: {item.FileName}");

                        Transform transform_XXX_To_RGB = null;

                        if (bitmap.PixelFormat == PixelFormat.Format24bppRgb)
                        {
                            transform_XXX_To_RGB = Transform.Create(inputProfile, Cms.TYPE_RGB_8, opt.DefaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_RGB_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
                        }
                        if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
                        {
                            transform_XXX_To_RGB = Transform.Create(inputProfile, Cms.TYPE_RGBA_8, opt.DefaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_RGB_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
                        }
                        if (bitmap.PixelFormat == PixelFormat.Format8bppIndexed)
                        {
                            transform_XXX_To_RGB = Transform.Create(inputProfile, Cms.TYPE_GRAY_8, opt.DefaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
                        }
                        if ((int)bitmap.PixelFormat == 0x200F)
                        {
                            transform_XXX_To_RGB = Transform.Create(inputProfile, Cms.TYPE_CMYK_8, opt.DefaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
                        }

                        if (transform_XXX_To_RGB != null)
                        {
                            using (transform_XXX_To_RGB)
                            {
                                Fill rect = new Fill(0, 0, bitmap.Width, bitmap.Height);
                                BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                                byte[] bitmapValues = new byte[Math.Abs(bitmapData.Stride) * bitmap.Height];
                                byte[] rgbValues;
                                Marshal.Copy(bitmapData.Scan0, bitmapValues, 0, bitmapValues.Length);
                                bitmap.UnlockBits(bitmapData);

                                using (Bitmap rgb = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
                                {
                                    BitmapData rgbData = rgb.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                                    rgbValues = new byte[Math.Abs(rgbData.Stride) * bitmap.Height];

                                    //StreamLogger.WriteLine($"Нормализация ImageID {item.Index}: Параметры Трансформации: Stride = {bitmapData.Stride}, Width = {bitmap.Width}, Height = {bitmap.Height}, PixelFormat = {bitmap.PixelFormat}. Файл: {item.FileName}");
                                    transform_XXX_To_RGB.DoTransform(bitmapValues, rgbValues, bitmap.Width, bitmap.Height, bitmapData.Stride, rgbData.Stride, bitmapData.Stride, rgbData.Stride);

                                    Marshal.Copy(rgbValues, 0, rgbData.Scan0, rgbValues.Length);
                                    rgb.UnlockBits(rgbData);

                                    rgb.Save(Path.Combine(tempFiles, item.GUID + ".jpg"), jpegImageCodecInfo, jpegEncoderParameters);

                                    item.AddStatus(ImageStatusEnum.ColorTransformed);
                                    if (item.ImageType != TagFileTypeEnum.JPEG)
                                    {
                                        item.AddStatus(ImageStatusEnum.FormatTransformed);
                                    }
                                    m.Add($"ImageID {item.Index}: Трансформация выполнена: {item.ColorSpace}:{bitmap.PixelFormat} -> {"RGB"}. Файл: {item.FileName} -> {item.GUID + ".jpg"}");
                                }
                            }
                        }
                        else
                        {
                            item.AddStatus(ImageStatusEnum.Error);
                            e.Add($"ImageID {item.Index}: Не определены необходимые параметры цветоделения для типа данных '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                        }
                    }
                    else
                    {
                        item.AddStatus(ImageStatusEnum.Error);
                        e.Add($"ImageID {item.Index}: Не определены необходимые профили цветоделения. Файл: {item.FileName}");
                    }
                }
            }
            catch (Exception ex)
            {
                item.AddStatus(ImageStatusEnum.Error);
                e.Add($"ImageID {item.Index}: ИСКЛЮЧЕНИЕ #2 '{ex.Message}'. Файл: {item.FileName}");
            }
            finally
            {
                if (inputProfile != null && inputProfile != opt.DefaultProfiles[item.ColorSpace]) inputProfile.Dispose();
            }
        }

        Log.WriteAll($"Нормализация (Поток {Thread.CurrentThread.ManagedThreadId})", m, w, e);

        opt.ResetEvent.Set();
    }
}

private void ObjectDetect(IEnumerable<ExifTaggedFile> _input)
{
    CascadeClassifier frontalface = null;
    CascadeClassifier profileface = null;

    List<string> messages = new List<string>();
    List<string> warnings = new List<string>();
    List<string> errors = new List<string>();

    DateTime start = DateTime.Now;

    try
    {
        frontalface = new CascadeClassifier(Path.Combine(MLData, "haarcascades", "haarcascade_frontalface_alt2.xml"));
        profileface = new CascadeClassifier(Path.Combine(MLData, "haarcascades", "haarcascade_profileface.xml"));

        foreach (var item in _input)
        {
            if (!item.HasStatus(ImageStatusEnum.Error) && !item.HasStatus(ImageStatusEnum.NotSupported))
            {
                try
                {
                    string file = Path.Combine(ProjectPath, "~Files", item.GUID + ".jpg");
                    if (!File.Exists(file))
                    {
                        warnings.Add($"File = {item.FileName}; Status = {item.Status}; Error = {"Файл не найден"}");
                        continue;
                    }
                    List<Fill> faces = new List<Fill>();

                    using (Mat image = new Mat(file))
                    {
                        using (UMat ugray = new UMat())
                        {
                            item.ObjectDetect.Clear();

                            CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                            CvInvoke.EqualizeHist(ugray, ugray);

                            Fill[] frontalfaceDetected = frontalface.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                            Fill[] profilefaceDetected = profileface.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));

                            for (int i = 0; i < frontalfaceDetected.Length; i++)
                            {
                                if (frontalfaceDetected[i].Width > 100 && frontalfaceDetected[i].Height > 100)
                                {
                                    faces.Add(frontalfaceDetected[i]);
                                }
                            }
                            for (int i = 0; i < profilefaceDetected.Length; i++)
                            {
                                if (profilefaceDetected[i].Width > 100 && profilefaceDetected[i].Height > 100)
                                {
                                    faces.Add(profilefaceDetected[i]);
                                }
                            }
                            if (faces.Count > 0)
                            {
                                item.AddStatus(ImageStatusEnum.FacesDetected);
                            }
                        }
                    }

                    for (int i = 0; i < faces.Count; i++)
                    {
                        bool isunnion = false;
                        for (int j = 0; j < item.ObjectDetect.Count; j++)
                        {
                            if (item.ObjectDetect[j].IntersectsWith(faces[i]))
                            {
                                item.ObjectDetect[j] = Fill.Union(item.ObjectDetect[j], faces[i]);
                                isunnion = true;
                                break;
                            }
                        }
                        if (!isunnion) item.ObjectDetect.Add(faces[i]);
                    }

                    foreach (var f in item.ObjectDetect)
                    {
                        messages.Add($"After union: File = {item.FileName}; Face rect: {f.X},{f.Y}; {f.Width}x{f.Height}");
                    }

                }
                catch (Exception ex)
                {
                    errors.Add($"File = {item.FileName}; Error = {ex.Message}");
                }
            }
        }
    }
    finally
    {
        if (frontalface != null) frontalface.Dispose();
        if (profileface != null) profileface.Dispose();
    }

    messages.Add($"Общее время обработки = {Math.Round((DateTime.Now - start).TotalSeconds)} сек");

    Log.WriteAll("Распознавание лиц", messages, warnings, errors);
}
*/
        /*
        private List<ImageToFrameData> GetSizeMatrix(IEnumerable<ExifTaggedFile> _input, int _minframes, float _k1 = 1.0f, float _k2 = 0.5f)
        {
            // Отбираем только те фреймы, которые используются хотя бы в одном шаблоне, у которого общее кол-во фреймов больше _minframes
            Size pageSize = new Size(200, 280);
            var actualTemplates = Product.LayoutSpace[pageSize].TemplateCollection;

            var f = actualTemplates.GetFrameAreas();
            var a = actualTemplates.GetFrameIndexes();

            var frameAreas = f.Where(x => x.Value.Value.Find(y => y.Frames.FramesCount > _minframes) != null).ToDictionary(x => x.Key, y => y.Value);
            var framesIndx = a.Where(x => x.Value.Value.Find(y => y.Frames.FramesCount > _minframes) != null).ToDictionary(x => x.Key, y => y.Value);

            // Миллиметры в квадрате
            int maxarea = frameAreas.Keys.Max(x => x.Width * x.Height);
            int minarea = frameAreas.Keys.Min(x => x.Width * x.Height);

            int dframes = maxarea - minarea;
            // Точки
            // Определяем по всему объему файлов, не только по текущему
            int maximage = InputData.Max(x => x.ImageSize.Width * x.ImageSize.Height);
            int minimage = InputData.Min(x => x.ImageSize.Width * x.ImageSize.Height);

            int dimages = maximage - minimage;

            var sorted = new List<ImageToFrameData>();

            foreach (var data in _input)
            {
                if (data.HasStatus(ImageStatusEnum.Error) || data.HasStatus(ImageStatusEnum.NotSupported)) continue;

                foreach (var area in frameAreas)
                {
                    // Единицы - точки
                    int Pi = data.ImageSize.Width * data.ImageSize.Height;
                    // Единицы - миллиметры в квадрате
                    int Pf = area.Key.Width * area.Key.Height;
                    float dRatio = 0f;
                    // Коффицент показывает насколько изображение сооответствует фрейму исходя из распределения внутри интервалов всех фреймов и изображений
                    float dSize = 0f;
                    // Коэффициент определяющий соответствие ориентации изображения и фрейма 
                    dRatio = Math.Abs((float)area.Key.Width / (float)area.Key.Height - (float)data.ImageSize.Width / (float)data.ImageSize.Height);

                    // Безразмерные отношения
                    float kImage = (float)(Pi - minimage) / (float)dimages; // точки
                    float kFrame = (float)(Pf - minarea) / (float)dframes; // миллиметры в квадрате
                    dSize = Math.Abs(kImage - kFrame); 
                    // Коффицент показывает насколько изображение сооответствует фрейму исходя из распределения внутри интервалов всех фреймов и изображений

                    sorted.Add(new ImageToFrameData() { image_id = data.Index, frame_id = area.Value.Key, dratio = dRatio * _k1, dsize = dSize * _k2 });
                }
            }

            sorted.Sort((x, y) => x.index.CompareTo(y.index));

            return sorted;
        }
        */
    }
}
