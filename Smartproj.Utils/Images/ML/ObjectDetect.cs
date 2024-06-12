using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Smartproj.Utils
{
    public class ObjectDetect
    {
        public string CascadesPath { get; set; }
        public Logger DetectLog { get; set; }
        public int SampleSize { get; set; }
        public float ScaleFactor { get; set; }
        public int MinHeighbors { get; set; }
        public ObjectDetectImageEnum ObjectDetectType { get; set; }
        public bool Detect(IEnumerable<ExifTaggedFile> _input, Func<ExifTaggedFile, string> _nameSelector, Dictionary<int, List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>>> _objectDetectedAreas)
        {
            if (_objectDetectedAreas == null || ObjectDetectType == ObjectDetectImageEnum.None) return false;

            CascadeClassifier frontalface = null;
            CascadeClassifier profileface = null;
            CascadeClassifier fullbody = null;
            CascadeClassifier upperbody = null;
            CascadeClassifier lowerbody = null;

            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();
            DateTime start = DateTime.Now;
            _objectDetectedAreas.Clear();
            bool hasErrors = false;

            try
            {
                if ((ObjectDetectType & ObjectDetectImageEnum.FrontFace) == ObjectDetectImageEnum.FrontFace) frontalface = new CascadeClassifier(Path.Combine(CascadesPath, "haarcascade_frontalface_alt2.xml"));
                if ((ObjectDetectType & ObjectDetectImageEnum.ProfileFace) == ObjectDetectImageEnum.ProfileFace) profileface = new CascadeClassifier(Path.Combine(CascadesPath, "haarcascade_profileface.xml"));
                if ((ObjectDetectType & ObjectDetectImageEnum.FullBody) == ObjectDetectImageEnum.FullBody) fullbody = new CascadeClassifier(Path.Combine(CascadesPath, "haarcascade_fullbody.xml"));
                if ((ObjectDetectType & ObjectDetectImageEnum.UpperBody) == ObjectDetectImageEnum.UpperBody) upperbody = new CascadeClassifier(Path.Combine(CascadesPath, "haarcascade_upperbody.xml"));
                if ((ObjectDetectType & ObjectDetectImageEnum.LowerBody) == ObjectDetectImageEnum.LowerBody) lowerbody = new CascadeClassifier(Path.Combine(CascadesPath, "haarcascade_lowerbody.xml"));

                foreach (var item in _input)
                {
                    if (!item.HasStatus(ImageStatusEnum.Error) && !item.HasStatus(ImageStatusEnum.NotSupported))
                    {
                        try
                        {
                            string file = _nameSelector(item);
                            if (!File.Exists(file))
                            {
                                item.AddStatus(ImageStatusEnum.Error);  
                                errors.Add($"File = {item.FileName}; Error = {"Файл не найден"}");
                                hasErrors = true;
                                continue;
                            }
                            List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>> objectsDetected = new List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>>();

                            using (Mat image = new Mat(file))
                            {
                                if (image.Width > image.Height)
                                {
                                    // Нормализация по ширине
                                    if (image.Width > SampleSize) CvInvoke.Resize(image, image, new Size(SampleSize, (SampleSize * image.Height) / image.Width));
                                }
                                else
                                {
                                    // Нормализация по высоте
                                    if (image.Height > SampleSize) CvInvoke.Resize(image, image, new Size((SampleSize * image.Width) / image.Height, SampleSize));
                                }

                                //using (UMat ugray = new UMat())
                                //CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                                //CvInvoke.EqualizeHist(ugray, ugray);

                                if (frontalface != null)
                                {
                                    List<RectangleF> frontalfaceDetected = frontalface.DetectMultiScale(image, ScaleFactor, MinHeighbors, new Size(30, 30)).Select(x => new RectangleF((float)x.X / image.Width, (float)x.Y / image.Height, (float)x.Width / image.Width, (float)x.Height / image.Height)).ToList();
                                    if (frontalfaceDetected.Count > 0)
                                    {
                                        objectsDetected.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(ObjectDetectImageEnum.FrontFace, frontalfaceDetected));
                                    }
                                }
                                if (profileface != null)
                                {
                                    List<RectangleF> profilefaceDetected = profileface.DetectMultiScale(image, ScaleFactor, MinHeighbors, new Size(30, 30)).Select(x => new RectangleF((float)x.X / image.Width, (float)x.Y / image.Height, (float)x.Width / image.Width, (float)x.Height / image.Height)).ToList();
                                    if (profilefaceDetected.Count > 0)
                                    {
                                        objectsDetected.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(ObjectDetectImageEnum.ProfileFace, profilefaceDetected));
                                    }
                                }
                                if (fullbody != null)
                                {
                                    List<RectangleF> fullbodyfaceDetected = fullbody.DetectMultiScale(image, ScaleFactor, MinHeighbors, new Size(30, 30)).Select(x => new RectangleF((float)x.X / image.Width, (float)x.Y / image.Height, (float)x.Width / image.Width, (float)x.Height / image.Height)).ToList();
                                    if (fullbodyfaceDetected.Count > 0)
                                    {
                                        objectsDetected.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(ObjectDetectImageEnum.FullBody, fullbodyfaceDetected));
                                    }
                                }
                                if (upperbody != null)
                                {
                                    List<RectangleF> upperbodyfaceDetected = upperbody.DetectMultiScale(image, ScaleFactor, MinHeighbors, new Size(30, 30)).Select(x => new RectangleF((float)x.X / image.Width, (float)x.Y / image.Height, (float)x.Width / image.Width, (float)x.Height / image.Height)).ToList();
                                    if (upperbodyfaceDetected.Count > 0)
                                    {
                                        objectsDetected.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(ObjectDetectImageEnum.UpperBody, upperbodyfaceDetected));
                                    }
                                }
                                if (lowerbody != null)
                                {
                                    List<RectangleF> lowerbodyfaceDetected = lowerbody.DetectMultiScale(image, ScaleFactor, MinHeighbors, new Size(30, 30)).Select(x => new RectangleF((float)x.X / image.Width, (float)x.Y / image.Height, (float)x.Width / image.Width, (float)x.Height / image.Height)).ToList();
                                    if (lowerbodyfaceDetected.Count > 0)
                                    {
                                        objectsDetected.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(ObjectDetectImageEnum.LowerBody, lowerbodyfaceDetected));
                                    }
                                }
                            }

                            if (objectsDetected.Count > 0)
                            {
                                var allForThisItem = new List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>>();
                                _objectDetectedAreas.Add(item.Index, allForThisItem);
                                item.AddStatus(ImageStatusEnum.FacesDetected);

                                for (int k = 0; k < objectsDetected.Count; k++) 
                                {
                                    allForThisItem.Add(new KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>(objectsDetected[k].Key, objectsDetected[k].Value.UnionAll()));
                                }

                                foreach (var f in allForThisItem)
                                {
                                    foreach (var d in f.Value)
                                    {
                                        messages.Add($"File = {item.FileName}; Type = {f.Key}; Face rect: {d.X.ToString("0.000")},{d.Y.ToString("0.000")}; {d.Width.ToString("0.000")}x{d.Height.ToString("0.000")}");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            item.AddStatus(ImageStatusEnum.Error);
                            errors.Add($"File = {item.FileName}; Error = {ex.Message}");
                            hasErrors = true;
                        }
                    }
                }
            }
            finally
            {
                if (frontalface != null) frontalface.Dispose();
                if (profileface != null) profileface.Dispose();
                if (fullbody != null) fullbody.Dispose();
                if (upperbody != null) upperbody.Dispose();
                if (lowerbody != null) lowerbody.Dispose();
            }

            messages.Add($"Общее время обработки = {Math.Round((DateTime.Now - start).TotalSeconds)} сек");

            if (DetectLog != null) DetectLog.WriteAll("ObjectDetect.Detect: Распознавание лиц", messages, warnings, errors);

            return !hasErrors;
        }
        public ObjectDetect()
        {
            ObjectDetectType = ObjectDetectImageEnum.DetectAll;
            SampleSize = 2000;
            ScaleFactor = 1.1f;
            MinHeighbors = 10;
        }
    }
}
