using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Smartproj
{
    public interface IQueueExtractor
    {
        RIP Owner { get; }
        Size Format { get; set; }
        List<ImageToFrameData> Extract(IEnumerable<ExifTaggedFile> _input, Predicate<Template> _templateFilter);
    }
    public struct ImageToFrameData
    {
        public float dsize;
        public float dratio;
        public int image_id;
        public int frame_id;
        public float index
        {
            get { return dratio + dsize; }
        }
    }
    public class MatrixExtractor : IQueueExtractor
    {
        public float K1;
        public float K2;
        public RIP Owner { get; }
        public Size Format { get; set; }
        public MatrixExtractor(RIP _rip) 
        {
            Owner = _rip;
        }
        public List<ImageToFrameData> Extract(IEnumerable<ExifTaggedFile> _input, Predicate<Template> _templateFilter)
        {
            // Отбираем только те фреймы, которые используются хотя бы в одном шаблоне, у которого общее кол-во фреймов больше _minframes
            var actualTemplates = Owner.Owner.LayoutSpace[Format].TemplateCollection;

            var f = actualTemplates.GetFrameAreas();
            var a = actualTemplates.GetFrameIndexes();

            var frameAreas = f.Where(x => x.Value.Value.Find(_templateFilter) != null).ToDictionary(x => x.Key, y => y.Value);
            var framesIndx = a.Where(x => x.Value.Value.Find(_templateFilter) != null).ToDictionary(x => x.Key, y => y.Value);

            // Миллиметры в квадрате
            int maxarea = frameAreas.Keys.Max(x => x.Width * x.Height);
            int minarea = frameAreas.Keys.Min(x => x.Width * x.Height);
            int dframes = maxarea - minarea;

            int criticalPixels = (int)(minarea * (Owner.CriticalResolution * Owner.CriticalResolution / 645.16f));
            // Точки
            // Определяем по всему объему файлов, не только по текущему
            var allImages = Owner.ImageProcessor.Items.Where(x => x.ImageSize.Width * x.ImageSize.Height > criticalPixels && !x.HasStatus(ImageStatusEnum.Error) && !x.HasStatus(ImageStatusEnum.NotSupported));

            int maximage = allImages.Max(x => x.ImageSize.Width * x.ImageSize.Height);
            int minimage = allImages.Min(x => x.ImageSize.Width * x.ImageSize.Height);
            int dimages = maximage - minimage;

            var sorted = new List<ImageToFrameData>();

            foreach (var data in _input)
            {
                if (data.HasStatus(ImageStatusEnum.Error) || data.HasStatus(ImageStatusEnum.NotSupported)) continue;
                // Единицы - точки
                int Pi = data.ImageSize.Width * data.ImageSize.Height;
                if (Pi < criticalPixels) continue;

                foreach (var area in frameAreas)
                {
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

                    sorted.Add(new ImageToFrameData() { image_id = data.Index, frame_id = area.Value.Key, dratio = dRatio * K1, dsize = dSize * K2 });
                }
            }

            sorted.Sort((x, y) => x.index.CompareTo(y.index));

            return sorted;
        }
    }
}
