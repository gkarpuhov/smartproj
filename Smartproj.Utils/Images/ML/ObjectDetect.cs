using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Smartproj.Utils
{
    public class ObjectDetect
    {
        public string CascadesPath { get; set; }
        public Logger DetectLog { get; set; }
        public ObjectDetectImageEnum ObjectDetectType { get; set; }
        public void Detect(IEnumerable<ExifTaggedFile> _input, Func<ExifTaggedFile, string> _nameSelector)
        {
            CascadeClassifier frontalface = null;
            CascadeClassifier profileface = null;
            CascadeClassifier fullbody = null;
            CascadeClassifier upperbody = null;
            CascadeClassifier lowerbody = null;

            List<string> messages = new List<string>();
            List<string> warnings = new List<string>();
            List<string> errors = new List<string>();
            DateTime start = DateTime.Now;
            
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
                                errors.Add($"File = {item.FileName}; Status = {item.Status}; Error = {"Файл не найден"}");
                                continue;
                            }
                            List<Rectangle> objectsDetected = new List<Rectangle>();

                            using (Mat image = new Mat(file))
                            {
                                using (UMat ugray = new UMat())
                                {
                                    item.ObjectDetect.Clear();

                                    CvInvoke.CvtColor(image, ugray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                                    CvInvoke.EqualizeHist(ugray, ugray);

                                    if (frontalface != null)
                                    {
                                        Rectangle[] frontalfaceDetected = frontalface.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                                        for (int i = 0; i < frontalfaceDetected.Length; i++)
                                        {
                                            if (frontalfaceDetected[i].Width > 100 && frontalfaceDetected[i].Height > 100)
                                            {
                                                objectsDetected.Add(frontalfaceDetected[i]);
                                            }
                                        }
                                    }
                                    if (profileface != null)
                                    {
                                        Rectangle[] profilefaceDetected = profileface.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                                        for (int i = 0; i < profilefaceDetected.Length; i++)
                                        {
                                            if (profilefaceDetected[i].Width > 100 && profilefaceDetected[i].Height > 100)
                                            {
                                                objectsDetected.Add(profilefaceDetected[i]);
                                            }
                                        }
                                    }
                                    if (fullbody != null)
                                    {
                                        Rectangle[] fullbodyfaceDetected = fullbody.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                                        for (int i = 0; i < fullbodyfaceDetected.Length; i++)
                                        {
                                            if (fullbodyfaceDetected[i].Width > 100 && fullbodyfaceDetected[i].Height > 100)
                                            {
                                                objectsDetected.Add(fullbodyfaceDetected[i]);
                                            }
                                        }
                                    }
                                    if (upperbody != null)
                                    {
                                        Rectangle[] upperbodyfaceDetected = upperbody.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                                        for (int i = 0; i < upperbodyfaceDetected.Length; i++)
                                        {
                                            if (upperbodyfaceDetected[i].Width > 100 && upperbodyfaceDetected[i].Height > 100)
                                            {
                                                objectsDetected.Add(upperbodyfaceDetected[i]);
                                            }
                                        }
                                    }
                                    if (lowerbody != null)
                                    {
                                        Rectangle[] lowerbodyfaceDetected = lowerbody.DetectMultiScale(ugray, 1.1, 10, new Size(20, 20));
                                        for (int i = 0; i < lowerbodyfaceDetected.Length; i++)
                                        {
                                            if (lowerbodyfaceDetected[i].Width > 100 && lowerbodyfaceDetected[i].Height > 100)
                                            {
                                                objectsDetected.Add(lowerbodyfaceDetected[i]);
                                            }
                                        }
                                    }
                                    if (objectsDetected.Count > 0)
                                    {
                                        item.AddStatus(ImageStatusEnum.FacesDetected);
                                    }
                                }
                            }

                            for (int i = 0; i < objectsDetected.Count; i++)
                            {
                                bool isunnion = false;
                                for (int j = 0; j < item.ObjectDetect.Count; j++)
                                {
                                    if (item.ObjectDetect[j].IntersectsWith(objectsDetected[i]))
                                    {
                                        item.ObjectDetect[j] = Rectangle.Union(item.ObjectDetect[j], objectsDetected[i]);
                                        isunnion = true;
                                        break;
                                    }
                                }
                                if (!isunnion) item.ObjectDetect.Add(objectsDetected[i]);
                            }

                            foreach (var f in item.ObjectDetect)
                            {
                                messages.Add($"File = {item.FileName}; Face rect: {f.X},{f.Y}; {f.Width}x{f.Height}");
                            }

                        }
                        catch (Exception ex)
                        {
                            errors.Add($"File = {item.FileName}; Error = {ex.Message}");
                            item.AddStatus(ImageStatusEnum.Error);
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

            if (DetectLog != null) DetectLog.WriteAll("Распознавание лиц", messages, warnings, errors);
        }
        public ObjectDetect()
        {
            ObjectDetectType = ObjectDetectImageEnum.FrontFace | ObjectDetectImageEnum.ProfileFace;
        }
    }
}
