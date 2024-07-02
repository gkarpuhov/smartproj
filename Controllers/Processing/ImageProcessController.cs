using GdPicture14;
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
using System.Xml.Serialization;
using static Smartproj.Utils.ColorUtils;
using Color = System.Drawing.Color;

namespace Smartproj
{
    public class ImageProcessControllerThreadOptions
    {
        public Dictionary<TagColorModeEnum, Profile> DefaultProfiles;
        public ConcurrentQueue<ExifTaggedFile> Queue;
        public Job Job;
        public ManualResetEvent ResetEvent;
        public ImageProcessController Controller;
        public bool HasErrors = false;
    }
    public class ImageProcessController : AbstractController
    {
        public override ProcessStatusEnum CurrentStatus { get; protected set; }
        [XmlElement]
        public ImageAreasEnum AreasDetection { get; set; }
        [XmlElement]
        public int SampleSize { get; set; }
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
                    Log?.WriteInfo("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал работу с процессом '{job.UID}'");
                  
                    Dictionary<TagColorModeEnum, Profile> defaultProfiles = new Dictionary<TagColorModeEnum, Profile>
                    {
                        { TagColorModeEnum.RGB, Profile.Open(Path.Combine(ws.Profiles, ColorImagesConverter.DefaultRGBProfile), "r") },
                        { TagColorModeEnum.CMYK, Profile.Open(Path.Combine(ws.Profiles, ColorImagesConverter.DefaultCMYKProfile), "r") },
                        { TagColorModeEnum.Grayscale, Profile.Open(Path.Combine(ws.Profiles, ColorImagesConverter.DefaultGRAYProfile), "r") },
                        { TagColorModeEnum.Lab, Profile.CreateLab4(Colorimetric.D50_xyY) }
                    };

                    ConcurrentQueue<ExifTaggedFile> queue = new ConcurrentQueue<ExifTaggedFile>(job.InputDataContainer);

                    int threadsCount = 6;
                    Thread[] pool = new Thread[threadsCount];
                    ManualResetEvent[] callback = new ManualResetEvent[threadsCount];
                    ImageProcessControllerThreadOptions[] processoptions = new ImageProcessControllerThreadOptions[threadsCount];

                    for (int i = 0; i < threadsCount; i++)
                    {
                        pool[i] = new Thread(parallelAction)
                        {
                            IsBackground = true
                        };
                        callback[i] = new ManualResetEvent(false);
                        pool[i].Start(processoptions[i] = new ImageProcessControllerThreadOptions() { Queue = queue, DefaultProfiles = defaultProfiles, Job = job, Controller = this, ResetEvent = callback[i] });
                    }

                    WaitHandle.WaitAll(callback);

                    foreach (var profile in defaultProfiles)
                    {
                        if (profile.Value != null) profile.Value.Dispose();
                    }

                    for (int i = 0; i < callback.Length; i++)
                    {
                        callback[i].Dispose();
                    }

                    if (!processoptions.Any(x => x.HasErrors))
                    {
                        Log?.WriteInfo("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
                    }

                    void parallelAction(object _object)
                    {
                        ImageProcessControllerThreadOptions opt = (ImageProcessControllerThreadOptions)_object;
                        ExifTaggedFile item = null;

                        while (opt.Queue.TryDequeue(out item))
                        {
                            using (GdPictureImaging oImage = new GdPictureImaging())
                            {
                                ExifTaggedFile imageData = item;

                                string imageFileType = $".{(opt.Job.Product.Optimization == FileSizeOptimization.Lossless ? "tiff" : "jpeg")}";
                                string imageFileName = imageData.GUID + imageFileType;
                                string imageFile = Path.Combine(opt.Job.JobPath, "~Files", imageFileName);
                                string maskPlace = Path.Combine(opt.Job.JobPath, "~Masks", imageData.GUID);

                                Directory.CreateDirectory(maskPlace);

                                int gdid = oImage.CreateGdPictureImageFromFile(imageFile);

                                try
                                {
                                    // Ограничение размеров. Нормализация до уровня SampleSize по большей стороне
                                    if (imageData.ImageSize.Width > imageData.ImageSize.Height)
                                    {
                                        // Нормализация по ширине
                                        if (imageData.ImageSize.Width > opt.Controller.SampleSize) oImage.ResizeWidthRatio(gdid, opt.Controller.SampleSize, System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic);
                                    }
                                    else
                                    {
                                        // Нормализация по высоте
                                        if (imageData.ImageSize.Height > opt.Controller.SampleSize) oImage.ResizeHeightRatio(gdid, opt.Controller.SampleSize, System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic);
                                    }
                                    //
                                    if (oImage.GetStride(gdid) < 0)
                                    {
                                        // Перевернутые сразу ставим на место. Похоже что GdPictureImaging всё открывет в перевернутом виде. Ну или почти все.
                                        // Делаем после нормализации! Ресампл почему то приводит изображение в правильный вид без дополнительной трансформации
                                        oImage.Rotate(gdid, RotateFlipType.Rotate180FlipX);
                                    }
                                    //
                                    int pixelsW = oImage.GetWidth(gdid);
                                    int pixelsH = oImage.GetHeight(gdid);
                                    int actualPixs = pixelsW * pixelsH;
                                    // Source RGB Buffer
                                    int stride = Math.Abs(oImage.GetStride(gdid));
                                    byte[] buffer = oImage.CopyToByteArray(gdid);
                                    // Source Lab Buffer
                                    byte[] labBuffer = new byte[6 * pixelsW * pixelsH];

                                    // New Empty Gray Image
                                    int grayId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, Color.White);
                                    int grayStride = oImage.GetStride(grayId);
                                    GdPictureDocumentUtilities.DisposeImage(grayId);
                                    //
                                    // Gray masks for areas
                                    // Область, исключающая белое пространство
                                    byte[] noWhiteMask = new byte[grayStride * pixelsH];
                                    // Область, исключающая белое пространство и сплошные заливки
                                    byte[] noWhiteAndFillMask = new byte[grayStride * pixelsH];
                                    // Область, включающая цвет кожи
                                    byte[] isSkinMask = new byte[grayStride * pixelsH];
                                    // Область, включающая только лица. В случае ч/б изображения содержит прямоугольные области ObjectDetectedAreas. Для цветного учитывается цвет кожи
                                    byte[] isFacesMask = new byte[grayStride * pixelsH];
                                    for (int k = 0; k < isSkinMask.Length; k++)
                                    {
                                        isSkinMask[k] = 255;
                                        isFacesMask[k] = 255;
                                    }
                                    // Счетчики пикселей
                                    int whiteCounter = 0;
                                    int greenCounter = 0;
                                    int blueCounter = 0;
                                    int fillCounter = 0;
                                    int skinCounter = 0;
                                    int grayCounter = 0;
                                    int sepiaCounter = 0;
                                    //
                                    DisjointSets fillDisjointSets = new DisjointSets();
                                    DisjointSets skinDisjointSets = new DisjointSets();
                                    //
                                    var transform_RGB_To_Lab = Transform.Create(opt.DefaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_BGR_8, opt.DefaultProfiles[TagColorModeEnum.Lab], Cms.TYPE_Lab_16, Intent.Perceptual, CmsFlags.None);

                                    using (transform_RGB_To_Lab)
                                    {
                                        // Трансформация в Lab
                                        transform_RGB_To_Lab.DoTransform(buffer, labBuffer, pixelsW, pixelsH, stride, pixelsW * 6, stride, pixelsW * 6);
                                        //
                                        // Распознанные области лиц
                                        List<RectangleF> allfaces = new List<RectangleF>();
                                        if (item.HasStatus(ImageStatusEnum.FacesDetected))
                                        {
                                            // Объеденяем пересекающиеся области
                                            List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>> allForThisItem;
                                            if (opt.Job.ProcessingSpace.ObjectDetectedAreas.TryGetValue(item.Index, out allForThisItem))
                                            {
                                                foreach (var pair in allForThisItem)
                                                {
                                                    if (pair.Key == ObjectDetectImageEnum.FrontFace || pair.Key == ObjectDetectImageEnum.ProfileFace) allfaces.AddRange(pair.Value);
                                                }
                                            }
                                            if (allfaces.Count > 0)
                                            {
                                                allfaces = allfaces.UnionAll();
                                            }
                                            // С самого начала надо посмотреть все распознанные области на предмет цвета:
                                            // Для того чтобы сделать вывод о цветности недостаточно иметь информацию о изображении в целом, может быть ситуация что человек (или его лицо) - ч/б, но кроме этого присутствуют другие цветные места,
                                            // тогда всё изображение не будет иметь статус "ч/б". А значит, анализируем каждую распознаную область независимо!
                                            // Если внутри область имеем только нейтраль, то просто добавляем ее целиком в маску, и удаляем из списка - больше никак не анализируем. 
                                            int j = 0;
                                            while (j < allfaces.Count)
                                            {
                                                Rectangle absoluteRectangle = new Rectangle((int)Math.Round(allfaces[j].X * pixelsW), (int)Math.Round(allfaces[j].Y * pixelsH), (int)Math.Round(allfaces[j].Width * pixelsW), (int)Math.Round(allfaces[j].Height * pixelsH));
                                                int colorCounter = 0;
                                                int colorLimit = 3;

                                                for (int x = absoluteRectangle.X; colorCounter < colorLimit && x <= absoluteRectangle.X + absoluteRectangle.Width; x++)
                                                {
                                                    for (int y = absoluteRectangle.Y; colorCounter < colorLimit && y <= absoluteRectangle.Y + absoluteRectangle.Height; y++)
                                                    {
                                                        int labShift = (y * pixelsW + x) * 6;
                                                        Pixel pixelData = new Pixel(new Point(x, y), new RGB(), ColorUtils.Lab.FromWordBufferToLab(labBuffer, labShift), 0);
                                                        // Сепию тоже учитываем
                                                        if ((pixelData.Flag & ColorPixelFlagEnum.Gray) != ColorPixelFlagEnum.Gray && (pixelData.Flag & ColorPixelFlagEnum.Sepia) != ColorPixelFlagEnum.Sepia)
                                                        {
                                                            colorCounter++;
                                                        }
                                                    }
                                                }
                                                if (colorCounter < colorLimit)
                                                {
                                                    // Менее colorLimit цветных точек. Добавляем всю область в маску
                                                    for (int x = absoluteRectangle.X; x <= absoluteRectangle.X + absoluteRectangle.Width; x++)
                                                    {
                                                        for (int y = absoluteRectangle.Y; y <= absoluteRectangle.Y + absoluteRectangle.Height; y++)
                                                        {
                                                            isFacesMask[y * grayStride + x] = 0;
                                                        }
                                                    }
                                                    // Удаляем из списка
                                                    allfaces.RemoveAt(j);
                                                }
                                                else
                                                {
                                                    j++;
                                                }
                                            }
                                        }

                                        // Далее полный анализ
                                        for (int j = 0; j < labBuffer.Length; j = j + 6)
                                        {
                                            Point pointXY = new Point((j % (pixelsW * 6)) / 6, j / (pixelsW * 6));
                                            int maskPosition = pointXY.Y * grayStride + pointXY.X;
                                            Pixel pixelData = new Pixel(pointXY, new RGB(), labBuffer.FromWordBufferToLab(j), 0);

                                            if ((pixelData.Flag & ColorPixelFlagEnum.White) != ColorPixelFlagEnum.White)
                                            {
                                                for (int x = -1; x <= 0; x++)
                                                {
                                                    for (int y = -1; y <= 0; y++)
                                                    {
                                                        if ((x != 0 || y != 0) && x + pointXY.X >= 0 && x + pointXY.X < pixelsW && y + pointXY.Y >= 0)
                                                        {
                                                            Point nextPoint = new Point(pointXY.X + x, pointXY.Y + y);

                                                            int nextShift = ((pointXY.Y + y) * pixelsW + (pointXY.X + x)) * 6;

                                                            ColorUtils.Lab nextLab = ColorUtils.Lab.FromWordBufferToLab(labBuffer, nextShift);
                                                            double weight = Math.Sqrt((nextLab.L - pixelData.Lab.L) * (nextLab.L - pixelData.Lab.L) + (nextLab.a - pixelData.Lab.a) * (nextLab.a - pixelData.Lab.a) + (nextLab.b - pixelData.Lab.b) * (nextLab.b - pixelData.Lab.b));

                                                            if (weight == 0)
                                                            {
                                                                int set1, set2;
                                                                if (!fillDisjointSets.IsInSameSet(pointXY, nextPoint, out set1, out set2))
                                                                {
                                                                    fillDisjointSets.Union(set1, set2);
                                                                }
                                                            }
                                                            if ((pixelData.Flag & ColorPixelFlagEnum.Skin) == ColorPixelFlagEnum.Skin && nextLab.IsSkinColor())
                                                            {
                                                                int set1, set2;
                                                                if (!skinDisjointSets.IsInSameSet(pointXY, nextPoint, out set1, out set2))
                                                                {
                                                                    skinDisjointSets.Union(set1, set2);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                //
                                                if ((pixelData.Flag & ColorPixelFlagEnum.Green) == ColorPixelFlagEnum.Green)
                                                {
                                                    greenCounter++;
                                                }
                                                if ((pixelData.Flag & ColorPixelFlagEnum.Sky) == ColorPixelFlagEnum.Sky)
                                                {
                                                    blueCounter++;
                                                }
                                                if ((pixelData.Flag & ColorPixelFlagEnum.Gray) == ColorPixelFlagEnum.Gray)
                                                {
                                                    grayCounter++;
                                                }
                                                else
                                                {
                                                    if ((pixelData.Flag & ColorPixelFlagEnum.Sepia) == ColorPixelFlagEnum.Sepia)
                                                    {
                                                        sepiaCounter++;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                noWhiteMask[maskPosition] = 255;
                                                noWhiteAndFillMask[maskPosition] = 255;
                                                whiteCounter++;
                                            }
                                        }

                                        var setsFillGroups = fillDisjointSets.GetData();
                                        // Маска сплошных заливок
                                        foreach (var set in setsFillGroups)
                                        {
                                            if (set.Value.Count > 1000 && set.Value.Frame.Width > 20 && set.Value.Frame.Height > 20)
                                            {
                                                foreach (var point in set.Value)
                                                {
                                                    int maskPosition = point.Y * grayStride + point.X;
                                                    noWhiteAndFillMask[maskPosition] = 255;
                                                    fillCounter++;
                                                }
                                            }
                                        }
                                        // Маска области кожи
                                        if (actualPixs - whiteCounter - grayCounter - sepiaCounter > 400)
                                        {
                                            var setsSkinGroups = skinDisjointSets.GetData();
                                            Dictionary<int, List<int>> faceareas = new Dictionary<int, List<int>>();

                                            foreach (var set in setsSkinGroups)
                                            {
                                                foreach (var point in set.Value)
                                                {
                                                    int maskPosition = point.Y * grayStride + point.X;
                                                    if (noWhiteAndFillMask[maskPosition] == 0)
                                                    {
                                                        // Исключаем область сплошной заливки
                                                        isSkinMask[maskPosition] = 0;
                                                        skinCounter++;

                                                        for (int j = 0; j < allfaces.Count; j++)
                                                        {
                                                            PointF normPoint = new PointF((float)point.X / (float)pixelsW, (float)point.Y / (float)pixelsH);
                                                            List<int> currarea = null;
                                                            if (!faceareas.TryGetValue(j, out currarea))
                                                            {
                                                                currarea = new List<int>();
                                                                faceareas.Add(j, currarea);
                                                            }
                                                            if (allfaces[j].ContainsF(normPoint))
                                                            {
                                                                currarea.Add(maskPosition);
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                            // Анализ области лиц. Предполагаем что лицо может оказаться не лицом. Скорре всего так будет при слишком маленькой доле заполнении области цветом кожи
                                            foreach (var pair in faceareas)
                                            {
                                                int absoluteW = (int)Math.Round(allfaces[pair.Key].Width * pixelsW);
                                                int absoluteH = (int)Math.Round(allfaces[pair.Key].Height * pixelsH);

                                                float kFillface = (float)pair.Value.Count / (absoluteW * absoluteH);

                                                if (kFillface > 0.3f)
                                                {
                                                    for (int k = 0; k < pair.Value.Count; k++)
                                                    {
                                                        isFacesMask[pair.Value[k]] = 0;
                                                    }
                                                }
                                                else
                                                {
                                                    // Удаляем из распознанных
                                                    foreach (var detected in opt.Job.ProcessingSpace.ObjectDetectedAreas[item.Index])
                                                    {
                                                        int j = 0;
                                                        while (j < detected.Value.Count)
                                                        {
                                                            if (detected.Value[j].IntersectsWith(allfaces[pair.Key]))
                                                            {
                                                                detected.Value.RemoveAt(j);
                                                            }
                                                            else
                                                            {
                                                                j++;
                                                            }
                                                        }
                                                    }
                                                    int k = 0;
                                                    while (k < opt.Job.ProcessingSpace.ObjectDetectedAreas[item.Index].Count)
                                                    {
                                                        if (opt.Job.ProcessingSpace.ObjectDetectedAreas[item.Index][k].Value.Count == 0)
                                                        {
                                                            opt.Job.ProcessingSpace.ObjectDetectedAreas[item.Index].RemoveAt(k);
                                                        }
                                                        else
                                                        {
                                                            k++;
                                                        }
                                                    }
                                                    if (opt.Job.ProcessingSpace.ObjectDetectedAreas[item.Index].Count == 0)
                                                    {
                                                        opt.Job.ProcessingSpace.ObjectDetectedAreas.Remove(item.Index);
                                                    }
                                                    // Подчистка
                                                }
                                            }
                                            //
                                            if (skinCounter > 400)
                                            {
                                                // Has Skin tones
                                            }
                                        }
                                        else
                                        {
                                            // Has RGB Gray
                                            // 20x20 (400) not gray pixels
                                        }

                                        // Размеры и разрешения файлов масок на данный момент сохраняются в нормализованном варианте, и являются формальными
                                        // Мы не знаем к каким параметрам далее будем приводить исходное изображение, это будет зависеть от того, куда оно будет вставлено
                                        // Таким образом, размеры масок должны быть далее скорректированы в соответствии с конечными параметрами изображения 
                                        if (noWhiteAndFillMask != null)
                                        {
                                            int noWhiteAndFillMaskId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, Color.Black);
                                            Marshal.Copy(noWhiteAndFillMask, 0, oImage.GetBits(noWhiteAndFillMaskId), noWhiteAndFillMask.Length);
                                            if (imageFileType == ".tiff")
                                            {
                                                oImage.SaveAsTIFF(noWhiteAndFillMaskId, Path.Combine(maskPlace, "_0.tiff"), TiffCompression.TiffCompressionLZW);
                                            }
                                            else
                                            {
                                                oImage.SaveAsJPEG(noWhiteAndFillMaskId, Path.Combine(maskPlace, "_0.jpeg"), 100);
                                            }
                                            GdPictureDocumentUtilities.DisposeImage(noWhiteAndFillMaskId);
                                        }
                                        if (noWhiteMask != null)
                                        {
                                            int noWhiteMaskId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, Color.Black);
                                            Marshal.Copy(noWhiteMask, 0, oImage.GetBits(noWhiteMaskId), noWhiteMask.Length);
                                            if (imageFileType == ".tiff")
                                            {
                                                oImage.SaveAsTIFF(noWhiteMaskId, Path.Combine(maskPlace, "_1.tiff"), TiffCompression.TiffCompressionLZW);
                                            }
                                            else
                                            {
                                                oImage.SaveAsJPEG(noWhiteMaskId, Path.Combine(maskPlace, "_1.jpeg"), 100);
                                            }
                                            GdPictureDocumentUtilities.DisposeImage(noWhiteMaskId);
                                        }
                                        if (isSkinMask != null)
                                        {
                                            int isSkinMaskId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, Color.Black);
                                            Marshal.Copy(isSkinMask, 0, oImage.GetBits(isSkinMaskId), isSkinMask.Length);
                                            if (imageFileType == ".tiff")
                                            {
                                                oImage.SaveAsTIFF(isSkinMaskId, Path.Combine(maskPlace, "_2.tiff"), TiffCompression.TiffCompressionLZW);
                                            }
                                            else
                                            {
                                                oImage.SaveAsJPEG(isSkinMaskId, Path.Combine(maskPlace, "_2.jpeg"), 100);
                                            }
                                            GdPictureDocumentUtilities.DisposeImage(isSkinMaskId);
                                        }
                                        if (isFacesMask != null)
                                        {
                                            int isFacesMaskId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, Color.Black);
                                            Marshal.Copy(isFacesMask, 0, oImage.GetBits(isFacesMaskId), isFacesMask.Length);
                                            if (imageFileType == ".tiff")
                                            {
                                                oImage.SaveAsTIFF(isFacesMaskId, Path.Combine(maskPlace, "_5.tiff"), TiffCompression.TiffCompressionLZW);
                                            }
                                            else
                                            {
                                                oImage.SaveAsJPEG(isFacesMaskId, Path.Combine(maskPlace, "_5.jpeg"), 100);
                                            }
                                            GdPictureDocumentUtilities.DisposeImage(isFacesMaskId);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    opt.HasErrors = true;
                                    opt.Job.Status = ProcessStatusEnum.Error;
                                    Log?.WriteError("ImageProcessController.Start", $"{opt.Job.Owner.ProjectId}: '{opt.Controller.GetType().Name}' => Обработанное исключение, файл '{imageData.GUID}'. Процесс '{opt.Job.UID}' прерван '{ex.Message}'");

                                }
                                finally
                                {
                                    oImage.ReleaseGdPictureImage(gdid);
                                }
                            }
                        }

                        opt.ResetEvent.Set();
                    }
                }
                catch (Exception ex)
                {
                    job.Status = ProcessStatusEnum.Error;
                    Log?.WriteError("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Обработанное исключение. Процесс '{job.UID}' прерван '{ex.Message}'");
                }
                finally
                {
                    CurrentStatus = ProcessStatusEnum.Finished;
                }
            }
            else
            {
                Log?.WriteInfo("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер деактивирован. Процессы не выполнены");
            }
        }
        protected override void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (CurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            }
            CurrentStatus = ProcessStatusEnum.Disposed;
        }
        public ImageProcessController() : base()
        {
            CurrentStatus = ProcessStatusEnum.New;
            AreasDetection = ImageAreasEnum.Skin;
            SampleSize = 2000;
            Enabled = false;
        }
    }
}
