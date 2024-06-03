using Newtonsoft.Json.Linq;
using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Smartproj
{
    public class ImposedDataContainer : IEnumerable<ImposedLayout>
    {
        private List<ImposedLayout> mItems;
        public Job Owner { get; }
        public int Count => mItems.Count;
        public ImposedLayout this[int _index] => mItems[_index];
        public ImposedDataContainer(Job _owner) 
        {
            Owner = _owner;
            mItems = new List<ImposedLayout>();
        }
        public ImposedLayout Add(ImposedLayout _item)
        {
            if (_item != null)
            {
                _item.Owner = this;
                mItems.Add(_item);
            }
            return _item;
        }
        public void Sort(Comparison<ImposedLayout> _comparison)
        {
            if (_comparison != null)
            {
                mItems.Sort(_comparison);
            }
        }
        public IEnumerator<ImposedLayout> GetEnumerator()
        {
            return mItems.GetEnumerator();  
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }
    public class ImposedImageData : IEqualityComparer<ImposedImageData>
    {
        public int FileId { get; set; }
        public PointF Shift { get; set; }
        public float Scale { get; set; }
        public int OrderBy { get; set; }
        public ImposedDataContainer Owner { get; }
        public ImposedImageData(ImposedDataContainer _owner)
        {
            Owner = _owner;
        }
        public bool Equals(ImposedImageData x, ImposedImageData y)
        {
            return x.FileId  != -1 && y.FileId != -1 && x.FileId == y.FileId;
        }
        public int GetHashCode(ImposedImageData obj)
        {
            return FileId.GetHashCode();
        }
    }
    public class ImposedLayout : IEnumerable<ImposedImageData>
    {
        public Template Templ { get; }
        public List<ImposedImageData[,]> Imposed { get; }
        public List<Segment> Segments { get; }
        public ImposedDataContainer Owner { get; set; }
        private int Safezone_F { get; set; } = 10;
        private int Safezone_C { get; set; } = 10;
        public double MinFileIdS { get; set; } = 1.0d;
        public int Available => Templ.Frames.FramesCount - this.Count();
        /*

        private int mItemsCount;



        private bool LayoutCorrect(IEnumerable<Rectangle> _safeAreas, Rectangle _frame, int _maxshift_X, int _maxshift_Y, int _bleed, out Point _shift)
        {
            return LayoutCorrect(new Size(Templ.Trim.Width.ToInt() / 2, Templ.Trim.Height.ToInt()), new ValueTuple<int, int>(Safezone_F, Safezone_C), _safeAreas, _frame, _maxshift_X, _maxshift_Y, _bleed, out _shift);
        }
        public static bool LayoutCorrect(Size _size, ValueTuple<int, int> _safezone, IEnumerable<Rectangle> _safeAreas, Rectangle _frame, int _maxshift_X, int _maxshift_Y, int _bleed, out Point _shift)
        {
            _shift = new Point(0, 0);

            //if (_safeAreas == null || _safeAreas.Count() == 0 || (_maxshift_X == 0 && _maxshift_Y == 0))
            if (_safeAreas == null || _safeAreas.Count() == 0)
            {
                return true;
            }

            int xshift = 0;
            int yshift = 0;
            int W = _size.Width;
            int H = _size.Height;
            int safezone_F = _safezone.Item1;
            int safezone_C = _safezone.Item2;
            //
            List<ValueTuple<Interval, Interval>> intervalsX = new List<ValueTuple<Interval, Interval>>();
            // Два типа интревалов при смещении по горизонтали, (условно левый и правый):
            // X1 - интервалы относящиеся к объектам на левой стороне разворота, отсутствие разворота, или интервал для которого не имеет значение расположение на развороте
            // X2 - интервалы рассчитанные из предположения что лицо должно находится целиком на правой стороне разворота, и для данного интервала это важно
            // При анализе интервалов, в случае когда определены оба интервала, есть возможность выбрать один из двух. Это даст больше вероятность найти область пересечения
            List<Interval> intervalsY = new List<Interval>();

            ValueTuple<int, int, int, int> frame = new ValueTuple<int, int, int, int>(_frame.X, _frame.Y, _frame.X + _frame.Width, _frame.Y + _frame.Height);

            foreach (var item in _safeAreas)
            {
                var face = new ValueTuple<int, int, int, int>(item.X, item.Y, item.X + item.Width, item.Y + item.Height);
                Interval dx_frame = null;
                Interval dx_cut_1 = null;
                Interval dx_cut_2 = null;
                Interval dy_frame = null;
                Interval dy_cut = null;

                // Предполагается что важны абсолютно все области лиц, и их обязательно надо поместить в живописное поле
                //if (_maxshift_X > 0)
                //{
                dx_frame = new Interval(safezone_F - face.Item1 + frame.Item1, frame.Item3 - face.Item3 - safezone_F);

                if (frame.Item3 <= _bleed + W || face.Item3 < _bleed + W)
                {
                    // 1. весь фрейм (или все лицо на фрейме) на левой стороне разворота  
                    dx_cut_1 = new Interval(safezone_C - face.Item1 + _bleed, _bleed + W - face.Item3 - safezone_C);
                }
                else
                {
                    // Фрейм переходит на правую сторону разворота (лицо или целиком на правой, или находит на корешок)
                    if (face.Item1 > _bleed + W)
                    {
                        // 2. лицо целиком на правой
                        dx_cut_1 = new Interval(safezone_C - face.Item1 + _bleed + W, _bleed + 2 * W - face.Item3 - safezone_C);
                    }
                    else
                    {
                        // 3. Если изображение лица изначально попало на корешок, есть вероятность его сдвинуть как в левую, так и в правую сторону
                        dx_cut_1 = new Interval(safezone_C - face.Item1 + _bleed, _bleed + W - face.Item3 - safezone_C);
                        dx_cut_2 = new Interval(safezone_C - face.Item1 + _bleed + W, _bleed + 2 * W - face.Item3 - safezone_C);
                    }
                }

                intervalsX.Add(new ValueTuple<Interval, Interval>(dx_frame, null));
                intervalsX.Add(new ValueTuple<Interval, Interval>(dx_cut_1, dx_cut_2));

                //}
                //if (_maxshift_Y > 0)
                //{
                dy_frame = new Interval(safezone_F - face.Item2 + frame.Item2, frame.Item4 - face.Item4 - safezone_F);
                dy_cut = new Interval(safezone_C - face.Item2 + _bleed, _bleed + H - face.Item4 - safezone_C);

                intervalsY.Add(dy_frame);
                intervalsY.Add(dy_cut);
                //}
            }
            // Поиск оптимального интервала при наличии лиц на корешке
            Interval iX = null;
            Interval iY = null;
            Interval minInterval = null;

            if (intervalsX.Count > 0)
            {
                Interval.Intersection(intervalsX.Select(x => x.Item1), out minInterval);

                for (int i = 0; i < intervalsX.Count; i++)
                {
                    if (intervalsX[i].Item2 != null)
                    {
                        Interval[] next = intervalsX.Select(x => x.Item1).ToArray();
                        for (int j = i; j < intervalsX.Count; j++)
                        {
                            if (intervalsX[j].Item2 != null)
                            {
                                Interval ix;
                                next[j] = intervalsX[j].Item2;
                                if (Interval.Intersection(next, out ix))
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

            if ((intervalsX.Count == 0 || minInterval != null) && (Interval.Intersection(intervalsY, out iY) || intervalsY.Count == 0))
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
                _shift = new Point(xshift, yshift);
                return true;
            }
            else
            {
                return false;
            }
        }


        public bool TryImageImpose(Size _frame, PageSide _pageside, int _value)
        {
            ExifTaggedFile file = Owner.Owner.DataContainer[_value];

            if (Available == 0) return false;
            if (!CheckDiatanceId(file.OrderBy)) return false;

            for (int i = 0; i < Templ.Frames.SidesCount; i++)
            {
                var frames = Templ.Frames[i];
                for (int k = 0; k < frames.GetLength(0); k++)
                {
                    for (int m = 0; m < frames.GetLength(1); m++)
                    {
                        // Размеры в миллиметрах
                        Rectangle frame = new Rectangle((int)Math.Round(frames[k, m].X), (int)Math.Round(frames[k, m].Y), (int)Math.Round(frames[k, m].Width), (int)Math.Round(frames[k, m].Height));
                        Size size = new Size(frame.Width, frame.Height);
                        if (size.Width == 0 && size.Height == 0) continue;
                        // Смещение не определяем так как нам не требуется разбивать развороты на страницы
                        var fitdata = frame.FitToFrame(file.ImageSize, 0);
                        float minResolution = 200f;
                        // Фреймы одинаковых размеров
                        // Проверяем на минимальное разрешение
                        if (size == _frame && Imposed[i][k, m].FileId == -1 && fitdata.Item2 >= minResolution / 25.4f)
                        {
                            // Анализ типа шаблона
                            if (Templ.Frames.IsSingle || _pageside == PageSide.Single || ((i == 0 && _pageside == PageSide.Left) || (i == 1 && _pageside == PageSide.Right)))
                            {
                                // Поиск сочетания фреймов и изображений, при котором лица попадут в неподходящее место
                                // Помещаем внутрь изображение
                                bool facesOutOfRange = false;
                                Point shift = new Point(0, 0);

                                if (file.ObjectDetect.Count > 0)
                                {
                                    // Области лиц в коллекции file.ObjectDetect имеют единицы пикселей и точки начала координат - левый верхний угол
                                    // Для анализа области нужно трансформировать в миллиметры и рабочую систему координат (левый нижний угол)
                                    IEnumerable<Rectangle> faceAreas = file.ObjectDetect.Select(face =>
                                    {
                                        // Трансформация масштабирования не требуется, так как размер в пикселях не изменился. Меняем точку отсчета Y
                                        // Единицы разрешения fitdata.Item2 - точки/мм (внутри области fitdata)
                                        // Координаты лиц относительно фрейма в миллиметрах
                                        int X_ABS = fitdata.Item1.X + (int)Math.Abs(((float)face.X / fitdata.Item2));
                                        int Y_ABS = fitdata.Item1.Y + (int)Math.Abs(((float)(file.ImageSize.Height - face.Y - face.Height) / fitdata.Item2));
                                        int W_ABS = (int)Math.Abs(((float)face.Width / fitdata.Item2));
                                        int H_ABS = (int)Math.Abs(((float)face.Height / fitdata.Item2));

                                        return new Rectangle(X_ABS, Y_ABS, W_ABS, H_ABS);
                                    });

                                    int maxShiftX = (fitdata.Item1.Width - frame.Width) / 2;
                                    int maxShiftY = (fitdata.Item1.Height - frame.Height) / 2;
                                    // Анализируем на предмет попадания лиц в проблемные зоны, и при необходимости попытка сдвинуть изображение для коррекции
                                    if (!LayoutCorrect(faceAreas, frame, maxShiftX, maxShiftY, (int)Templ.Bleed, out shift))
                                    {
                                        facesOutOfRange = true;
                                    }
                                    else
                                    {
                                    }
                                }

                                if (!facesOutOfRange)
                                {
                                    Imposed[i][k, m].FileId = _value;
                                    Imposed[i][k, m].Shift = shift;
                                    mItemsCount++;

                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
        */
        public ImposedLayout(Template _templ, params Segment[] _seg)
        {
            Templ = _templ;
            Imposed = new List<ImposedImageData[,]>();
            for (int i = 0; i < _templ.Frames.SidesCount; i++)
            {
                int w = _templ.Frames[i].GetLength(0);
                int h = _templ.Frames[i].GetLength(1);
                var frames = new ImposedImageData[w, h];

                for (int k = 0; k < w; k++)
                {
                    for (int m = 0; m < h; m++)
                    {
                        frames[k, m] = new ImposedImageData(Owner) { FileId = -1, Shift = new Point(0, 0), Scale = 1.0f, OrderBy = 0 };
                    }
                }
                Imposed.Add(frames);
            }
            //PageCount = _pages;
            //mItemsCount = 0;
            Segments = new List<Segment>();
            if (_seg != null)
            {
                Segments.AddRange(_seg);
            }
        }
        private bool CheckDiatanceId(int _id)
        {
            List<double> ids = new List<double>() { _id };

            foreach (var id in this)
            {
                ids.Add(Owner.Owner.DataContainer[id.FileId].OrderBy);
            }

            if (ids.Count <= 1) return true;

            return (ids.S1() / (double)ids.Count) <= MinFileIdS;
        }
        public IEnumerator<ImposedImageData> GetEnumerator()
        {
            for (int i = 0; i < Imposed.Count; i++)
            {
                foreach (ImposedImageData item in Imposed[i])
                {
                    if (item.FileId != -1) yield return item;
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
