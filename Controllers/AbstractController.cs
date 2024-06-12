using Emgu.CV.Face;
using ProjectPage;
using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Текущее состояние процесса контроллера.
    /// Статус Stopping актуален только для реализации контроллера AbstractInputProvider. Определяет состояние пока происходит остановки выполяемых процессов
    /// </summary>
    public enum ProcessStatusEnum
    {
        New = 0,
        Initalizing,
        Processing,
        Stopping,
        Idle,
        Error,
        Finished,
        Disposed
    }
    /// <summary>
    /// Общий интерфейс для любых обработчиков данных. Относиться к определенному экземпляру проекта
    /// </summary>
    public interface IController : IDisposable
    {
        ControllerCollection Owner { get; set; }
        /// <summary>
        /// Свойство определяет порядок запуска в синхронной общей очереди на обработку
        /// </summary>
        int Priority { get; set; }
        /// <summary>
        /// Если свойство имеет значение false, то при вызове метода <see cref="Start"/> никаие действия не будут выполнены, и метод вернет значение false
        /// </summary>
        bool Enabled { get; set; }
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        Guid UID { get; }
        /// <summary>
        /// Текущий статус выполнения процессов контроллером
        /// </summary>
        ProcessStatusEnum CurrentStatus { get; }
        /// <summary>
        /// Начинает выполнение определенного для данного контроллера, процесса
        ///  Если свойство Enabled имеет значение false, то при вызове метода никаиrе действия не будут выполнены, и метод вернет значение false
        /// </summary>
        void Start(object[] _settings);
    }
    /// <summary>
    /// Интерфейс для определения способа выдачи конечного результата обработки в определенный объект назначения. Расширяет функциональность контроллера
    /// </summary>
    public interface IOutputProvider
    {
        /// <summary>
        /// Строка, определеющая путь для вывода данных
        /// </summary>
        string Destination { get; }
    }
    /// <summary>
    /// Интерфейс для определения способа получения исходных данных и параметров для обработки задания. Расширяет функциональность контроллера
    /// </summary>
    public interface IInputProvider
    {
        Guid AdapterId { get; }
        /// <summary>
        /// Строка, определеющая путь источника получения данных
        /// </summary>
        string Source { get; }
        /// <summary>
        /// Коллекция контроллеров, реализующих логику интерфейса <see cref="IInputProvider"/>. Определянт механизм вывода результата обработки. Предназначена для глобального применения ко всем заданияем, созданным данным объектом IInputProvider.
        /// Кроме экземпляров данной коллекции, подобные контроллеры могут быть добавлены и на локальном уровне продукта. Все они будут отработаны
        /// </summary>
        ControllerCollection DefaultOutput { get; }
        /// <summary>
        /// Остановка выполнеия контроллера. Контроллер должен дождаться завершения текущего процесса обработки и остановиться. После этого новые задания на обработку больше не инициируются.
        /// Метод должен освободить ресурсы
        /// </summary>
        /// <returns>Объект <see cref="IAsyncResult"/> является состоянием внутренних асинхронных процессов</returns>
        IAsyncResult Stop();
    }
    /// <summary>
    /// Абстрактная реализация обработчика данных
    /// </summary>
    public abstract class AbstractController : IController
    {
        /// <summary>
        /// Набор параметров, переданных в метод Start
        /// </summary>
        public object[] StartParameters { get; protected set; }
        /// <summary>
        /// Описание контроллера
        /// </summary>
        [XmlElement]
        public string Label { get; set; }
        /// <summary>
        /// Уникальный идентификатор
        /// </summary>
        [XmlElement]
        public Guid UID { get; protected set; }
        /// <summary>
        /// Свойство определяет порядок запуска в синхронной общей очереди на обработку
        /// </summary>
        [XmlElement]
        public int Priority { get; set; }
        /// <summary>
        /// Если свойство имеет значение false, то при вызове метода <see cref="Start"/> никакие действия не будут выполнены, и метод вернет значение false
        /// </summary>
        [XmlElement]
        public bool Enabled { get; set; }
        public ControllerCollection Owner { get; set; }
        /// <summary>
        /// Текущий статус выполнения процессов контроллером
        /// </summary>
        public abstract ProcessStatusEnum CurrentStatus { get; protected set; }
        public Logger Log => Owner?.Log;
        public static void TryImageFit(ImageFrame _graphic, ImposedImageData _data)
        {
            var toTryinmpse = _data.Owner;
            var job = toTryinmpse.Owner.Owner;

            List<RectangleF> allfaces = new List<RectangleF>();
            if (job.InputDataContainer[_data.FileId].HasStatus(ImageStatusEnum.FacesDetected))
            {
                List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>> allForThisItem;
                if (job.ProcessingSpace.ObjectDetectedAreas.TryGetValue(_data.FileId, out allForThisItem))
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

            if ((_graphic.AutoFitObjectType & AutoPositionObjectTypeEnum.OneFace) == AutoPositionObjectTypeEnum.OneFace && allfaces.Count == 1)
            {
                float faceWidht = 0;
                float faceHeiht = 0;
                float frame_face_dx = 0;
                float frame_face_dy = 0;
                // из нормализованного размера в абсолютный
                RectangleF detectedFace = new RectangleF(job.InputDataContainer[_data.FileId].ImageSize.Width * allfaces[0].X, job.InputDataContainer[_data.FileId].ImageSize.Height * allfaces[0].Y, job.InputDataContainer[_data.FileId].ImageSize.Width * allfaces[0].Width, job.InputDataContainer[_data.FileId].ImageSize.Height * allfaces[0].Height);
                // точек/мм
                float resolution = 0;

                if (_graphic.AutoDetectAndFocusWidth > 0)
                {
                    // в миллиметры
                    faceWidht = _graphic.AutoDetectAndFocusWidth;
                    resolution = detectedFace.Width / faceWidht;
                    faceHeiht = detectedFace.Height / resolution;
                    // Позиционирование лицо во фрейм
                    // Координаты  области лица в миллиметрах относительно левого нижнего угла фрейма
                    if ((_graphic.AutoFitPaddling & PositionEnum.Left) == PositionEnum.Left)
                    {
                        frame_face_dx = _graphic.AutoFitMargins.Left;
                    }
                    if ((_graphic.AutoFitPaddling & PositionEnum.Right) == PositionEnum.Right)
                    {
                        frame_face_dx = _graphic.Bounds.Width - _graphic.AutoFitMargins.Right - faceWidht;
                    }
                    if ((_graphic.AutoFitPaddling & PositionEnum.CenterHorizontal) == PositionEnum.CenterHorizontal)
                    {
                        frame_face_dx = _graphic.AutoFitMargins.Left + (_graphic.Bounds.Width - _graphic.AutoFitMargins.Left - _graphic.AutoFitMargins.Right - faceWidht) / 2;
                    }
                    if ((_graphic.AutoFitPaddling & PositionEnum.Bottom) == PositionEnum.Bottom)
                    {
                        frame_face_dy = _graphic.AutoFitMargins.Bottom;
                    }
                    if ((_graphic.AutoFitPaddling & PositionEnum.Top) == PositionEnum.Top)
                    {
                        frame_face_dy = _graphic.Bounds.Height - _graphic.AutoFitMargins.Top - faceHeiht;
                    }
                    if ((_graphic.AutoFitPaddling & PositionEnum.CenterVertical) == PositionEnum.CenterVertical)
                    {
                        frame_face_dy = _graphic.AutoFitMargins.Bottom + (_graphic.Bounds.Height - _graphic.AutoFitMargins.Bottom - _graphic.AutoFitMargins.Top - faceHeiht) / 2;
                    }
                }
                else
                {
                    // Впихиваем целиком лицо в доступную область фрейма
                    if (detectedFace.Width / detectedFace.Height > (_graphic.Bounds.Width - _graphic.AutoFitMargins.Right - _graphic.AutoFitMargins.Left) / (_graphic.Bounds.Height - _graphic.AutoFitMargins.Bottom - _graphic.AutoFitMargins.Top))
                    {
                        // Выравниваем лицо по ширине области
                        faceWidht = _graphic.Bounds.Width - _graphic.AutoFitMargins.Right - _graphic.AutoFitMargins.Left;
                        resolution = detectedFace.Width / faceWidht;
                        faceHeiht = detectedFace.Height / resolution;
                        //
                        frame_face_dx = _graphic.AutoFitMargins.Left;
                        //
                        if ((_graphic.AutoFitPaddling & PositionEnum.Bottom) == PositionEnum.Bottom)
                        {
                            frame_face_dy = _graphic.AutoFitMargins.Bottom;
                        }
                        if ((_graphic.AutoFitPaddling & PositionEnum.Top) == PositionEnum.Top)
                        {
                            frame_face_dy = _graphic.Bounds.Height - _graphic.AutoFitMargins.Top - faceHeiht;
                        }
                        if ((_graphic.AutoFitPaddling & PositionEnum.CenterVertical) == PositionEnum.CenterVertical)
                        {
                            frame_face_dy = _graphic.AutoFitMargins.Bottom + (_graphic.Bounds.Height - _graphic.AutoFitMargins.Bottom - _graphic.AutoFitMargins.Top - faceHeiht) / 2;
                        }
                    }
                    else
                    {
                        // По высоте
                        faceHeiht = _graphic.Bounds.Height - _graphic.AutoFitMargins.Top - _graphic.AutoFitMargins.Bottom;
                        resolution = detectedFace.Height / faceHeiht;
                        faceWidht = detectedFace.Width / resolution;
                        //
                        frame_face_dy = _graphic.AutoFitMargins.Bottom;
                        //
                        if ((_graphic.AutoFitPaddling & PositionEnum.Left) == PositionEnum.Left)
                        {
                            frame_face_dx = _graphic.AutoFitMargins.Left;
                        }
                        if ((_graphic.AutoFitPaddling & PositionEnum.Right) == PositionEnum.Right)
                        {
                            frame_face_dx = _graphic.Bounds.Width - _graphic.AutoFitMargins.Right - faceWidht;
                        }
                        if ((_graphic.AutoFitPaddling & PositionEnum.CenterHorizontal) == PositionEnum.CenterHorizontal)
                        {
                            frame_face_dx = _graphic.AutoFitMargins.Left + (_graphic.Bounds.Width - _graphic.AutoFitMargins.Left - _graphic.AutoFitMargins.Right - faceWidht) / 2;
                        }
                    }
                }

                if (resolution * 25.4 < job.MinimalResolution)
                {
                    job.Log?.WriteError("VruAdapter.GetNext", $"resolution < job.MinimalResolution");
                }

                // Размер изображения в миллиметрах
                float widht = job.InputDataContainer[_data.FileId].ImageSize.Width / resolution;
                float height = job.InputDataContainer[_data.FileId].ImageSize.Height / resolution;
                // Смещение левого нижнего угла лица относительно левого нижнего угла изображения в миллиметрах
                float image_face_dx = detectedFace.X / resolution;
                float image_face_dy = (job.InputDataContainer[_data.FileId].ImageSize.Height - detectedFace.Height - detectedFace.Y) / resolution;
                // Смещение изображения относительно фрейма в миллиметрах
                float image_frame_dx = image_face_dx - frame_face_dx;
                float image_frame_dy = image_face_dy - frame_face_dy;
                // Проверка что изображение не вышло из фрейма
                if (image_frame_dx >= 0 && image_frame_dy >= 0 && image_frame_dx + _graphic.Bounds.Width <= widht && image_frame_dy + _graphic.Bounds.Height <= height)
                {
                    // Сохраняем область изображения
                    _data.Bounds = new RectangleF(_graphic.Bounds.X - image_frame_dx, _graphic.Bounds.Y - image_frame_dy, widht, height);
                }
                else
                {
                    job.Log?.WriteError("VruAdapter.GetNext", $"image_frame_dx >= 0 && image_frame_dy >= 0 && image_frame_dx + _graphic.Bounds.Width <= widht && image_frame_dy + _graphic.Bounds.Height <= height");
                }

                return;
            }
            
            var fitedRect = _graphic.Bounds.FitToFrameF(job.InputDataContainer[_data.FileId].ImageSize.Width, job.InputDataContainer[_data.FileId].ImageSize.Height);
            if (fitedRect.Item2 * 25.4 < job.MinimalResolution)
            {
                // Error
            }

            if (_graphic.AutoFitObjectType == AutoPositionObjectTypeEnum.Off || allfaces.Count == 0)
            {
                // Просто вставка без условий
                _data.Bounds = fitedRect.Item1;
                return;
            }

            if (_graphic.AutoFitObjectType == AutoPositionObjectTypeEnum.ProtectFaces)
            {
                PointF shift;
                if (LayoutCorrect(_graphic, _data, out shift))
                {
                    _data.Bounds = new RectangleF(fitedRect.Item1.X + shift.X, fitedRect.Item1.Y + shift.Y, fitedRect.Item1.Width, fitedRect.Item1.Height);
                }
                else
                {
                    // Error
                }
                return;
            }

            if ((_graphic.AutoFitObjectType & AutoPositionObjectTypeEnum.GroupFaces) == AutoPositionObjectTypeEnum.GroupFaces && allfaces.Count > 1)
            {

            }
        }
        public static bool LayoutCorrect(ImageFrame _graphic, ImposedImageData _data, out PointF _shift)
        {
            var toTryinmpse = _data.Owner;
            var job = toTryinmpse.Owner.Owner;
            SizeF size = toTryinmpse.Templ.Owner.Owner.ProductSize;
            float safezoneCut = toTryinmpse.Templ.SafeCutZone;
            float safezoneFrame = _graphic.SafeFrameZone;
            float bleed = toTryinmpse.Templ.Bleed;
            RectangleF frame = _graphic.Bounds;
            var fitedRect = _graphic.Bounds.FitToFrameF(job.InputDataContainer[_data.FileId].ImageSize.Width, job.InputDataContainer[_data.FileId].ImageSize.Height);

            float maxShiftX = (fitedRect.Item1.Width - frame.Width) / 2;
            float maxShiftY = (fitedRect.Item1.Height - frame.Height) / 2;

            List<RectangleF> faceAreas = new List<RectangleF>();

            if (job.InputDataContainer[_data.FileId].HasStatus(ImageStatusEnum.FacesDetected))
            {
                List<RectangleF> allfaces = new List<RectangleF>();
                if (job.InputDataContainer[_data.FileId].HasStatus(ImageStatusEnum.FacesDetected))
                {
                    List<KeyValuePair<ObjectDetectImageEnum, List<RectangleF>>> allForThisItem;
                    if (job.ProcessingSpace.ObjectDetectedAreas.TryGetValue(_data.FileId, out allForThisItem))
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

                foreach (var facerect in allfaces)
                {
                    // из нормализованного размера в абсолютный
                    RectangleF detectedFace = new RectangleF(job.InputDataContainer[_data.FileId].ImageSize.Width * facerect.X, job.InputDataContainer[_data.FileId].ImageSize.Height * facerect.Y, job.InputDataContainer[_data.FileId].ImageSize.Width * facerect.Width, job.InputDataContainer[_data.FileId].ImageSize.Height * facerect.Height);
                    // Меняем точку отсчета Y
                    // Единицы разрешения fitedRect.Item2 - точки/мм
                    // Координаты лиц относительно фрейма в миллиметрах
                    float X_ABS = fitedRect.Item1.X + Math.Abs(detectedFace.X / fitedRect.Item2);
                    float Y_ABS = fitedRect.Item1.Y + Math.Abs((job.InputDataContainer[_data.FileId].ImageSize.Height - detectedFace.Y - detectedFace.Height) / fitedRect.Item2);
                    float W_ABS = Math.Abs(detectedFace.Width / fitedRect.Item2);
                    float H_ABS = Math.Abs(detectedFace.Height / fitedRect.Item2);
                    faceAreas.Add(new RectangleF(X_ABS, Y_ABS, W_ABS, H_ABS));
                }
            }

            return LayoutCorrect(size, safezoneFrame, safezoneCut, faceAreas, frame, maxShiftX, maxShiftY, bleed, out _shift);
        }

        private static bool LayoutCorrect(SizeF _size, float _safezoneFrame, float _safezoneCut, IEnumerable<RectangleF> _safeAreas, RectangleF _frame, float _maxshift_X, float _maxshift_Y, float _bleed, out PointF _shift)
        {
            /* 
            _safeAreas - Зоны распознаных лиц в миллиметрах. Координаты - левый нижний угол относительно левого нижнего угла всего pdf документа (обрезного формата)
            _frame - Область фрейма шаблона для вставки изображения
            _maxshift_X, _maxshift_Y - Максимально возможный сдвиг изображения относительно фрейма
            _size - Формат продукта (полосы)
            */

            _shift = new PointF(0, 0);

            if (_safeAreas == null || _safeAreas.Count() == 0)
            {
                return true;
            }

            float xshift = 0;
            float yshift = 0;
            float W = _size.Width;
            float H = _size.Height;
            //
            List<ValueTuple<IntervalF, IntervalF>> intervalsX = new List<ValueTuple<IntervalF, IntervalF>>();
            // Два типа интревалов при смещении по горизонтали, (условно левый и правый):
            // X1 - интервалы относящиеся к объектам на левой стороне разворота, отсутствие разворота, или интервал для которого не имеет значение расположение на развороте
            // X2 - интервалы рассчитанные из предположения что лицо должно находится целиком на правой стороне разворота, и для данного интервала это важно
            // При анализе интервалов, в случае когда определены оба интервала, есть возможность выбрать один из двух. Это даст больше вероятность найти область пересечения
            List<IntervalF> intervalsY = new List<IntervalF>();

            ValueTuple<float, float, float, float> frame = new ValueTuple<float, float, float, float>(_frame.X, _frame.Y, _frame.X + _frame.Width, _frame.Y + _frame.Height);

            foreach (var item in _safeAreas)
            {
                var face = new ValueTuple<float, float, float, float>(item.X, item.Y, item.X + item.Width, item.Y + item.Height);
                IntervalF dx_frame = null;
                IntervalF dx_cut_1 = null;
                IntervalF dx_cut_2 = null;
                IntervalF dy_frame = null;
                IntervalF dy_cut = null;

                // Предполагается что важны абсолютно все области лиц, и их обязательно надо поместить в живописное поле

                dx_frame = new IntervalF(_safezoneFrame - face.Item1 + frame.Item1, frame.Item3 - face.Item3 - _safezoneFrame);

                if (frame.Item3 <= _bleed + W || face.Item3 < _bleed + W)
                {
                    // 1. весь фрейм (или все лицо на фрейме) на левой стороне разворота  
                    dx_cut_1 = new IntervalF(_safezoneCut - face.Item1 + _bleed, _bleed + W - face.Item3 - _safezoneCut);
                }
                else
                {
                    // Фрейм переходит на правую сторону разворота (лицо или целиком на правой, или находит на корешок)
                    if (face.Item1 > _bleed + W)
                    {
                        // 2. лицо целиком на правой
                        dx_cut_1 = new IntervalF(_safezoneCut - face.Item1 + _bleed + W, _bleed + 2 * W - face.Item3 - _safezoneCut);
                    }
                    else
                    {
                        // 3. Если изображение лица изначально попало на корешок, есть вероятность его сдвинуть как в левую, так и в правую сторону
                        dx_cut_1 = new IntervalF(_safezoneCut - face.Item1 + _bleed, _bleed + W - face.Item3 - _safezoneCut);
                        dx_cut_2 = new IntervalF(_safezoneCut - face.Item1 + _bleed + W, _bleed + 2 * W - face.Item3 - _safezoneCut);
                    }
                }

                intervalsX.Add(new ValueTuple<IntervalF, IntervalF>(dx_frame, null));
                intervalsX.Add(new ValueTuple<IntervalF, IntervalF>(dx_cut_1, dx_cut_2));


                dy_frame = new IntervalF(_safezoneFrame - face.Item2 + frame.Item2, frame.Item4 - face.Item4 - _safezoneFrame);
                dy_cut = new IntervalF(_safezoneCut - face.Item2 + _bleed, _bleed + H - face.Item4 - _safezoneCut);

                intervalsY.Add(dy_frame);
                intervalsY.Add(dy_cut);
            }

            // Поиск оптимального интервала при наличии лиц на корешке
            IntervalF iX = null;
            IntervalF iY = null;
            IntervalF minInterval = null;

            if (intervalsX.Count > 0)
            {
                IntervalF.Intersection(intervalsX.Select(x => x.Item1), out minInterval);

                for (int i = 0; i < intervalsX.Count; i++)
                {
                    if (intervalsX[i].Item2 != null)
                    {
                        IntervalF[] next = intervalsX.Select(x => x.Item1).ToArray();
                        for (int j = i; j < intervalsX.Count; j++)
                        {
                            if (intervalsX[j].Item2 != null)
                            {
                                IntervalF ix;
                                next[j] = intervalsX[j].Item2;
                                if (IntervalF.Intersection(next, out ix))
                                {
                                    if (minInterval == null || Math.Min(Math.Abs(ix.X1), Math.Abs(ix.X2)) < Math.Min(Math.Abs(minInterval.X1), Math.Abs(minInterval.X2)))
                                    {
                                        minInterval = ix;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            bool inrange = false;

            if ((intervalsX.Count == 0 || minInterval != null) && (IntervalF.Intersection(intervalsY, out iY) || intervalsY.Count == 0))
            {
                inrange = true;

                if (inrange && intervalsX.Count > 0)
                {
                    iX = minInterval;
                    if (!iX.Contains(0)) xshift = (Math.Abs(iX.X1) < Math.Abs(iX.X2) ? iX.X1 : iX.X2);
                    inrange = Math.Abs(xshift) <= _maxshift_X;
                }
                if (inrange && intervalsY.Count > 0)
                {
                    if (!iY.Contains(0)) yshift = (Math.Abs(iY.X1) < Math.Abs(iY.X2) ? iY.X1 : iY.X2);
                    inrange = Math.Abs(yshift) <= _maxshift_Y;
                }
            }

            if (inrange)
            {
                _shift = new PointF(xshift, yshift);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        protected AbstractController()
        {
            UID = Guid.NewGuid();
            Enabled = true;
        }
        /// <summary>
        /// Начинает выполнение определенного для данного контроллера, процесса
        /// </summary>
        /// <param name="_settings"></param>
        public abstract void Start(object[] _settings);
        /// <summary>
        /// Стандартный метод для переопределения механизма освобождения внутренних ресурсов
        /// Если свойство Enabled имеет значение false, то при вызове метода никаиrе действия не будут выполнены, и метод вернет значение false
        /// </summary>
        /// <param name="_disposing"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        protected abstract void Dispose(bool _disposing);
        /// <summary>
        /// Деструктор по умолчанию
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~AbstractController()
        {
            Dispose(false);
        }
    }
}
