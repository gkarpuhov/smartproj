using lcmsNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Smartproj.Utils
{
    public class ProcessThreadOptions
    {
        public Dictionary<TagColorModeEnum, Profile> DefaultProfiles;
        public ConcurrentQueue<ExifTaggedFile> Queue;
        public string TempPath;
        public ManualResetEvent ResetEvent;
        public bool HasErrors = false;
    }
    public class ColorImagesConverter
    {
        private TagFileTypeEnum mOutType;
        public string DefaultRGBProfile { get; set; }
        public string DefaultCMYKProfile { get; set; }
        public string DefaultGRAYProfile { get; set; }
        public string DefaultLABProfile { get; set; }
        public string DefaultColorPath { get; set; }
        public string ProfilesPath { get; set; }
        public string OutPath { get; set; }
        public long QualityParameter { get; set; }

        public TagFileTypeEnum OutType
        {
            get { return mOutType; } 
            set
            {
                if (value == TagFileTypeEnum.JPEG || value == TagFileTypeEnum.TIFF)
                {
                    mOutType = value;
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
        }
        public Logger ConverterLog { get; set; }
        public ColorImagesConverter()
        {
            DefaultColorPath = @"C:\Windows\System32\spool\drivers\color";
            DefaultRGBProfile = "sRGB Color Space Profile.icm";
            DefaultCMYKProfile = "ISOcoated_v2_eci.icc";
            DefaultGRAYProfile = "Generic Gray Gamma 2.2 Profile.icc";
            DefaultLABProfile = "lab1to1.icc";
            OutType = TagFileTypeEnum.JPEG;
            QualityParameter = 100L;
        }
        public bool Process(IEnumerable<ExifTaggedFile> _inputData)
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

            if (!Directory.Exists(Path.Combine(OutPath, "~Files")))
            {
                Directory.CreateDirectory(Path.Combine(OutPath, "~Files"));
            }
            if (!Directory.Exists(Path.Combine(OutPath, "~Cms")))
            {
                Directory.CreateDirectory(Path.Combine(OutPath, "~Cms"));
            }

            foreach (var name in new string[] { DefaultRGBProfile, DefaultCMYKProfile, DefaultGRAYProfile, DefaultLABProfile })
            {
                if (!File.Exists(Path.Combine(ProfilesPath, name)) && File.Exists(Path.Combine(DefaultColorPath, name)))
                {
                    File.Copy(Path.Combine(DefaultColorPath, name), Path.Combine(ProfilesPath, name));
                }
            }

            if (File.Exists(Path.Combine(ProfilesPath, DefaultRGBProfile)))
            {
                defaultProfiles[TagColorModeEnum.RGB] = Profile.Open(Path.Combine(ProfilesPath, DefaultRGBProfile), "r");
                var id = defaultProfiles[TagColorModeEnum.RGB].HeaderProfileID;
                string description = defaultProfiles[TagColorModeEnum.RGB].GetProfileInfo(InfoType.Description, "en", "US");
                messages.Add($"{DefaultRGBProfile} => Использован RGB профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.RGB].Version};  '[{String.Join(" ", id)}] [{description}]'");
            }
            if (File.Exists(Path.Combine(ProfilesPath, DefaultCMYKProfile)))
            {
                defaultProfiles[TagColorModeEnum.CMYK] = Profile.Open(Path.Combine(ProfilesPath, DefaultCMYKProfile), "r");
                var id = defaultProfiles[TagColorModeEnum.CMYK].HeaderProfileID;
                string description = defaultProfiles[TagColorModeEnum.CMYK].GetProfileInfo(InfoType.Description, "en", "US");
                messages.Add($"{DefaultCMYKProfile} => Использован CMYK профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.CMYK].Version};  '[{String.Join(" ", id)}] [{description}]'");
            }
            if (File.Exists(Path.Combine(ProfilesPath, DefaultGRAYProfile)))
            {
                defaultProfiles[TagColorModeEnum.Grayscale] = Profile.Open(Path.Combine(ProfilesPath, DefaultGRAYProfile), "r");
                var id = defaultProfiles[TagColorModeEnum.Grayscale].HeaderProfileID;
                string description = defaultProfiles[TagColorModeEnum.Grayscale].GetProfileInfo(InfoType.Description, "en", "US");
                messages.Add($"{DefaultGRAYProfile} => Использован Grayscale профиль по умолчанию: Ver = {defaultProfiles[TagColorModeEnum.Grayscale].Version}; '[{String.Join(" ", id)}] [{description}]'");
            }

            ConcurrentQueue<ExifTaggedFile> queue = new ConcurrentQueue<ExifTaggedFile>(_inputData);
            var options = new ProcessThreadOptions() { Queue = queue, DefaultProfiles = defaultProfiles, TempPath = OutPath };

            int threadsCount = 4;
            Thread[] pool = new Thread[threadsCount];
            ManualResetEvent[] callback = new ManualResetEvent[threadsCount];
            ProcessThreadOptions[] processoptions = new ProcessThreadOptions[threadsCount];

            DateTime start = DateTime.Now;

            for (int i = 0; i < threadsCount; i++)
            {
                pool[i] = new Thread(parallelAction)
                {
                    IsBackground = true
                };
                callback[i] = new ManualResetEvent(false);
                pool[i].Start(processoptions[i] = new ProcessThreadOptions() { Queue = queue, DefaultProfiles = defaultProfiles, TempPath = OutPath, ResetEvent = callback[i]});
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

            ConverterLog?.WriteAll("ColorImagesConverter.Process: Нормализация", messages, warnings, errors);

            return !processoptions.Any(x => x.HasErrors);

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
                    
                    ImageCodecInfo imageCodecInfo;
                    EncoderParameters encoderParameters;
                    EncoderParameter encoderParameter1;

                    if (OutType == TagFileTypeEnum.TIFF)
                    {
                        imageCodecInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/tiff");
                        encoderParameter1 = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionNone);
                    }
                    else
                    {
                        imageCodecInfo = ImageCodecInfo.GetImageEncoders().First(i => i.MimeType == "image/jpeg");
                        encoderParameter1 = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, QualityParameter);
                    }

                    encoderParameters = new EncoderParameters(1);
                    encoderParameters.Param[0] = encoderParameter1;

                    if (item.ColorSpace != TagColorModeEnum.RGB && item.ColorSpace != TagColorModeEnum.CMYK && item.ColorSpace != TagColorModeEnum.Grayscale)
                    {
                        item.AddStatus(ImageStatusEnum.NotSupported);
                        e.Add($"ID {item.Index}: Цветового пространство не поддерживается: '{item.ColorSpace}'. Файл: {item.FileName}");
                        opt.HasErrors = true;
                    }
                    if (item.HasTransparency)
                    {
                        w.Add($"ID {item.Index}: Обнаружено наличие прозрачности (будет удалена). Файл: {item.FileName}");
                    }
                    if (item.Bpc != 8)
                    {
                        item.AddStatus(ImageStatusEnum.NotSupported);
                        e.Add($"ID {item.Index}: Требуется формат 8 бит на пиксель: Обнаружено '{item.ColorSpace}:{item.Bpc}'. Задача прервана. Файл: {item.FileName}");
                        opt.HasErrors = true;
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
                                e.Add($"ID {item.Index}: ОШИБКА - Некорректный формат цветового профиля: Ожидается '{item.ColorSpace}', обнаружен '{inputProfile.ColorSpace}'. Файл: {item.FileName}");
                                opt.HasErrors = true;
                            }

                            isDefaultProfile = CMS.CompareProfiles(inputProfile, opt.DefaultProfiles[item.ColorSpace]);
                            if (isDefaultProfile)
                            {
                                m.Add($"ID {item.Index}: Цветовой профиль источника соответствует профилю по умолчанию. Файл: {item.FileName}");
                            }
                        }
                        else
                        {
                            item.AddStatus(ImageStatusEnum.Error);
                            e.Add($"ID {item.Index}: ОШИБКА - Ошибка данных цветового профиля. Файл: {item.FileName}");
                            opt.HasErrors = true;
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
                            if (item.ImageType == OutType) 
                            {
                                File.Copy(Path.Combine(item.FilePath, item.FileName), Path.Combine(tempFiles, $"{item.GUID}.{OutType.ToString().ToLower()}"));
                                item.AddStatus(ImageStatusEnum.OriginalIsReady);
                                m.Add($"ID {item.Index}: Не требуется. Файл: {item.FileName} -> {item.GUID}.{OutType.ToString().ToLower()}");
                            }
                            else
                            {
                                using (Bitmap bitmap = new Bitmap(Path.Combine(item.FilePath, item.FileName), false))
                                {
                                    if ((bitmap.PixelFormat == PixelFormat.Format24bppRgb && !item.HasTransparency) || (bitmap.PixelFormat == PixelFormat.Format32bppArgb && item.HasTransparency))
                                    {
                                        bitmap.Save(Path.Combine(tempFiles, $"{item.GUID}.{OutType.ToString().ToLower()}"), imageCodecInfo, encoderParameters);
                                        
                                        item.AddStatus(ImageStatusEnum.FormatTransformed);
                                        m.Add($"ID {item.Index}: Формат изменен: {item.ImageType.ToString()} -> {OutType}. Файл: {item.FileName} -> {item.GUID}.{OutType.ToString().ToLower()}");
                                    }
                                    else
                                    {
                                        item.AddStatus(ImageStatusEnum.Error);
                                        e.Add($"ID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства: Ожидается '{"Format24bppRgb или PixelFormat.Format32bppArgb"}'. Обнаружен '{bitmap.PixelFormat}' (HasTransparency = {item.HasTransparency}). Файл: {item.FileName}");
                                        opt.HasErrors = true;
                                    }
                                }
                            }

                            continue;
                        }
                        catch (Exception ex)
                        {
                            item.AddStatus(ImageStatusEnum.Error);
                            e.Add($"ID {item.Index}: ИСКЛЮЧЕНИЕ #1 '{ex.Message}'. Файл: {item.FileName}");
                            opt.HasErrors = true;
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
                                        e.Add($"ID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства RGB: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                                        opt.HasErrors = true;
                                    }
                                    break;
                                case TagColorModeEnum.CMYK:
                                    if ((int)bitmap.PixelFormat != 0x200F)
                                    {
                                        item.AddStatus(ImageStatusEnum.Error);
                                        e.Add($"ID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства CMYK: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                                        opt.HasErrors = true;
                                    }
                                    break;
                                case TagColorModeEnum.Grayscale:
                                    if (bitmap.PixelFormat != PixelFormat.Format8bppIndexed)
                                    {
                                        item.AddStatus(ImageStatusEnum.Error);
                                        e.Add($"ID {item.Index}: ОШИБКА - Неожиданный формат цветового пространства GRAY: '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                                        opt.HasErrors = true;
                                    }
                                    break;
                            }

                            if (item.HasStatus(ImageStatusEnum.Error)) continue;

                            if (inputProfile != null && opt.DefaultProfiles[TagColorModeEnum.RGB] != null)
                            {
                                var id = inputProfile.HeaderProfileID;
                                string description = inputProfile.GetProfileInfo(InfoType.Description, "en", "US");

                                m.Add($"ID {item.Index}: Использован входной профиль {ColorSpaceSignature.RgbData}: Ver = {inputProfile.Version}; '[{String.Join(" ", id)}] [{description}]'. Файл: {item.FileName}");

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
                                        Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                                        BitmapData bitmapData = bitmap.LockBits(rect, ImageLockMode.ReadWrite, bitmap.PixelFormat);

                                        byte[] bitmapValues = new byte[Math.Abs(bitmapData.Stride) * bitmap.Height];
                                        byte[] rgbValues;
                                        Marshal.Copy(bitmapData.Scan0, bitmapValues, 0, bitmapValues.Length);
                                        bitmap.UnlockBits(bitmapData);

                                        using (Bitmap rgb = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format24bppRgb))
                                        {
                                            BitmapData rgbData = rgb.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                                            rgbValues = new byte[Math.Abs(rgbData.Stride) * bitmap.Height];

                                            transform_XXX_To_RGB.DoTransform(bitmapValues, rgbValues, bitmap.Width, bitmap.Height, bitmapData.Stride, rgbData.Stride, bitmapData.Stride, rgbData.Stride);

                                            Marshal.Copy(rgbValues, 0, rgbData.Scan0, rgbValues.Length);
                                            rgb.UnlockBits(rgbData);

                                            rgb.Save(Path.Combine(tempFiles, $"{item.GUID}.{OutType.ToString().ToLower()}"), imageCodecInfo, encoderParameters);

                                            item.AddStatus(ImageStatusEnum.ColorTransformed);
                                            if (item.ImageType != OutType)
                                            {
                                                item.AddStatus(ImageStatusEnum.FormatTransformed);
                                            }
                                            m.Add($"ID {item.Index}: Трансформация выполнена: {item.ColorSpace}:{bitmap.PixelFormat} -> {"RGB"}. Файл: {item.FileName} -> {item.GUID}.{OutType.ToString().ToLower()}");
                                        }
                                    }
                                }
                                else
                                {
                                    item.AddStatus(ImageStatusEnum.Error);
                                    e.Add($"ID {item.Index}: Не определены необходимые параметры цветоделения для типа данных '{bitmap.PixelFormat}'. Файл: {item.FileName}");
                                    opt.HasErrors = true;
                                }
                            }
                            else
                            {
                                item.AddStatus(ImageStatusEnum.Error);
                                e.Add($"ID {item.Index}: Не определены необходимые профили цветоделения. Файл: {item.FileName}");
                                opt.HasErrors = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        item.AddStatus(ImageStatusEnum.Error);
                        e.Add($"ID {item.Index}: ИСКЛЮЧЕНИЕ #2 '{ex.Message}'. Файл: {item.FileName}");
                        opt.HasErrors = true;
                    }
                    finally
                    {
                        if (inputProfile != null && inputProfile != opt.DefaultProfiles[item.ColorSpace]) inputProfile.Dispose();
                    }
                }

                ConverterLog?.WriteAll($"ColorImagesConverter.Process: Нормализация (Поток {Thread.CurrentThread.ManagedThreadId})", m, w, e);

                opt.ResetEvent.Set();
            }
        }
    }
}
