using Emgu.CV;
using GdPicture14;
using lcmsNET;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.Xml.Serialization;
using Telegram.Bot.Types;
using static Smartproj.Utils.ColorUtils;
using static System.Net.Mime.MediaTypeNames;

namespace Smartproj
{
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

                    using (GdPictureImaging oImage = new GdPictureImaging())
                    {
                        for (int i = 0; i < job.InputDataContainer.Count; i++)
                        {
                            ExifTaggedFile imageData = job.InputDataContainer[i];
                            string imageFile = Path.Combine(job.JobPath, "~Files", imageData.GUID + $".{(job.Product.Optimization == FileSizeOptimization.Lossless ? "tiff" : "jpeg")}");
                            int gdid = oImage.CreateGdPictureImageFromFile(imageFile);

                            try
                            {
                                if (imageData.ImageSize.Width > imageData.ImageSize.Height)
                                {
                                    // Нормализация по ширине
                                    if (imageData.ImageSize.Width > SampleSize) oImage.Resize(gdid, SampleSize, (SampleSize * imageData.ImageSize.Height) / imageData.ImageSize.Width, System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic);
                                }
                                else
                                {
                                    // Нормализация по высоте
                                    if (imageData.ImageSize.Height > SampleSize) oImage.Resize(gdid, (SampleSize * imageData.ImageSize.Width) / imageData.ImageSize.Height, SampleSize, System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic);
                                }
                                
                                //
                                int pixelsW = oImage.GetWidth(gdid);
                                int pixelsH = oImage.GetHeight(gdid);
                                // Source RGB Buffer
                                int stride = Math.Abs(oImage.GetStride(gdid));
                                byte[] buffer = oImage.CopyToByteArray(gdid);
                                // Source Lab Buffer
                                byte[] labBuffer = new byte[6 * pixelsW * pixelsH];

                                // New Empty Gray Image
                                int grayId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, System.Drawing.Color.White);
                                int grayStride = Math.Abs(oImage.GetStride(grayId));
                                GdPictureDocumentUtilities.DisposeImage(grayId);

                                byte[] isSkinMask = new byte[grayStride * pixelsH];
                                for (int k = 0; k < isSkinMask.Length; k++)
                                {
                                    isSkinMask[k] = 255;
                                }

                                var transform_RGB_To_Lab = Transform.Create(defaultProfiles[TagColorModeEnum.RGB], Cms.TYPE_BGR_8, defaultProfiles[TagColorModeEnum.Lab], Cms.TYPE_Lab_16, Intent.Perceptual, CmsFlags.None);

                                using (transform_RGB_To_Lab)
                                {
                                    transform_RGB_To_Lab.DoTransform(buffer, labBuffer, pixelsW, pixelsH, stride, pixelsW * 6, stride, pixelsW * 6);
                                    //
                                    if ((AreasDetection & ImageAreasEnum.Skin) == ImageAreasEnum.Skin)
                                    {
                                        DisjointSets skinDisjointSets = new DisjointSets();
                                        //Log?.WriteInfo("ImageProcessController.Start", $"ID {i}: Анализ и кластеризация... Файл: {imageData.FileName}");

                                        for (int j = 0; j < labBuffer.Length; j = j + 6)
                                        {
                                            Point pointXY = new Point((j % (pixelsW * 6)) / 6, j / (pixelsW * 6));
                                            Pixel pixelData = new Pixel(pointXY, new RGB(), labBuffer.FromWordBufferToLab(j), 0);

                                            if ((pixelData.Flag & ColorPixelFlagEnum.Skin) == ColorPixelFlagEnum.Skin)
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

                                                            if (nextLab.IsSkinColor())
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
                                            }
                                        }

                                        var setsSkinGroups = skinDisjointSets.GetData();
                                        int clindex = 0;
                                        float normFactor = (float)imageData.ImageSize.Width / pixelsW;
                                        List<RectangleF> allfaces = new List<RectangleF>();
                                        if (job.InputDataContainer[i].HasStatus(ImageStatusEnum.FacesDetected))
                                        {
                                            List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>> allForThisItem;
                                            if (job.ProcessingSpace.ObjectDetectedAreas.TryGetValue(i, out allForThisItem))
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
                                        }

                                        foreach (var set in setsSkinGroups)
                                        {
                                            if (set.Value.Count > 500 && set.Value.Frame.Width > 20 && set.Value.Frame.Height > 20)
                                            {
                                                foreach (var facerect in allfaces)
                                                {
                                                    // из нормализованного размера в абсолютный
                                                    RectangleF detectedFace = new RectangleF(imageData.ImageSize.Width * facerect.X, imageData.ImageSize.Height * facerect.Y, imageData.ImageSize.Width * facerect.Width, imageData.ImageSize.Height * facerect.Height);
                                                    // из нормализованного размера в абсолютный
                                                    RectangleF skinArea = new RectangleF(set.Value.Frame.X * normFactor, set.Value.Frame.Y * normFactor, set.Value.Frame.Width * normFactor, set.Value.Frame.Height * normFactor);
                                                    if (detectedFace.IntersectsWith(skinArea))
                                                    {
                                                        Log?.WriteInfo("IntersectsWith", $"{imageData.FileName}; Skin: {skinArea.X.ToString("0")},{skinArea.Y.ToString("0")}; {skinArea.Width.ToString("0")}x{skinArea.Height.ToString("0")}; Face: {detectedFace.X.ToString("0")},{detectedFace.Y.ToString("0")}; {detectedFace.Width.ToString("0")}x{detectedFace.Height.ToString("0")}");
                                                    }
                                                }
                                                foreach (var point in set.Value)
                                                {
                                                    int maskPosition = point.Y * grayStride + point.X;
                                                    isSkinMask[maskPosition] = 0;
                                                }
                                                /*
                                                int skinCounter = 0;
                                                foreach (var point in set.Value)
                                                {
                                                    //int maskPosition = point.Y * grayStride + point.X;
                                                    skinCounter++;
                                                }
                                                Log?.WriteInfo("ImageProcessController.Start", $"ID {i}: Кластер: {clindex}; Объём = {skinCounter}: Файл: {imageData.FileName}");
                                                */
                                                clindex++;
                                            }
                                        }
                                        Log?.WriteInfo("IntersectsWith", $"Name = {imageData.FileName}; Stride = {oImage.GetStride(gdid)}");
                                        int isSkinMaskId = oImage.CreateNewGdPictureImage(pixelsW, pixelsH, PixelFormat.Format8bppIndexed, System.Drawing.Color.Black);
                                        Marshal.Copy(isSkinMask, 0, oImage.GetBits(isSkinMaskId), isSkinMask.Length);
                                        oImage.SaveAsJPEG(isSkinMaskId, $@"c:\Temp\{imageData.FileName}.jpg", 80);
                                        GdPictureDocumentUtilities.DisposeImage(isSkinMaskId);
                                    }
                                }
                            }
                            finally
                            {
                                oImage.ReleaseGdPictureImage(gdid);
                            }
                        }
                    }

                    foreach (var profile in defaultProfiles)
                    {
                        profile.Value.Dispose();
                    }
                    if (false)
                    {
                        job.Status = ProcessStatusEnum.Error;
                        Log?.WriteError("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Ошибка при выполненнии процесса '{job.UID}'");
                    }
                    else
                    {
                        Log?.WriteInfo("ImageProcessController.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер завершил работу с процессом '{job.UID}'");
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
