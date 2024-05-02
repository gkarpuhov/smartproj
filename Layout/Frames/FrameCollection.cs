using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Smartproj
{
    public class FrameAddedEventArgs : EventArgs
    {
        public FrameAddedEventArgs(RectangleF _area, PageSide _side, Point _position, IEnumerable<TextFrame> _textParagraph)
        {
            Area = _area;
            Side = _side;
            Position = _position;
            Owner = null;
            PinnedParagraphs = _textParagraph;   
        }
        public readonly RectangleF Area;
        public readonly PageSide Side;
        public readonly Point Position;
        public readonly Template Owner;
        public readonly IEnumerable<TextFrame> PinnedParagraphs;
        public ImageFrame NewImageFrame { get; set; }
    }
    public class FrameCollectionNavigator
    {
        public FrameCollectionNavigator(FrameCollection _owner)
        {
            Items = _owner;
            mSide = 0;
            mFrame = -1;
        }
        public readonly FrameCollection Items;
        private int mSide;
        private int mFrame;
        public RectangleF Current
        {
            get
            {
                if (mFrame == -1) return default(RectangleF);
                var sideFrames = Items[mSide];
                return sideFrames[mFrame / sideFrames.GetLength(1), mFrame % sideFrames.GetLength(1)];
            }
        }
        public int Side => mSide;
        public PageSide PageSide => Items.IsSingle ? PageSide.Single : (mSide == 0 ? PageSide.Left : PageSide.Right);
        public bool MoveNext()
        {
            while (true)
            {
                var sideFrames = Items[mSide];

                if (mFrame + 1 < sideFrames.Length)
                {
                    mFrame++;
                    if (sideFrames[mFrame / sideFrames.GetLength(1), mFrame % sideFrames.GetLength(1)] != default)
                    {
                        return true;
                    }
                }
                else
                {
                    if (mSide + 1 < Items.SidesCount)
                    {
                        mSide++;
                        mFrame = -1;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        public void Reset()
        {
            mSide = 0;
            mFrame = -1;
        }
    }
    /// <summary>
    /// Абстрактный контейнер, определяющий расположение и позиционирование изображений в шаблоне
    /// </summary>
    public abstract class FrameCollection : IEnumerable<RectangleF>
    {
        private List<RectangleF[,]> mFrames;
        private FrameCollectionNavigator mNavigator;
        /// <summary>
        /// Ссылка на контейнер <see cref="Template"/>
        /// </summary>
        public Template Owner { get; }
        /// <summary>
        /// Определяет, является ли шаблон разворотным или нет
        /// </summary>
        public bool IsSingle { get; }
        /// <summary>
        /// Количество, определенных в шаблоне фреймов
        /// </summary>
        public abstract int FramesCount { get; }
        /// <summary>
        /// Количество сторон шаблона. Для разворотного шаблона равно 2, в ином случае 1
        /// </summary>
        public int SidesCount => mFrames.Count;
        /// <summary>
        /// Ссылка на экземпляр итератора фреймов в коллеции <see cref="FrameCollection"/>
        /// </summary>
        public FrameCollectionNavigator Navigator => mNavigator;
        /// <summary>
        /// Объект логирования. Ссылка на экземпляр контейнера <see cref="Template.Log"/>
        /// </summary>
        public Logger Log => Owner?.Log;
        /// <summary>
        /// Конструктор создания разворотного шаблона
        /// </summary>
        /// <param name="_leftW"></param>
        /// <param name="_leftH"></param>
        /// <param name="_rightW"></param>
        /// <param name="_rightH"></param>
        /// <param name="_owner"></param>
        protected FrameCollection(int _leftW, int _leftH, int _rightW, int _rightH, Template _owner)
        {
            IsSingle = false;
            _owner.LeftW = _leftW;
            _owner.LeftH = _leftH;
            _owner.RightW = _rightW;
            _owner.RightH = _rightH;

            mFrames = new List<RectangleF[,]>
            {
                // Первый индекс столбцы, вторая строки.
                // Перебор по умолчанию идет сначала по строке (rank 1), потом переходит на следующий столбец (rank 0)
                new RectangleF[_leftH, _leftW],
                new RectangleF[_rightH, _rightW]
            };
            mNavigator = new FrameCollectionNavigator(this);
            Owner = _owner;
        }
        /// <summary>
        /// Конструктор создания однополосного шаблона
        /// </summary>
        /// <param name="_singleW"></param>
        /// <param name="_singleH"></param>
        /// <param name="_owner"></param>
        protected FrameCollection(int _singleW, int _singleH, Template _owner)
        {
            IsSingle = true;
            _owner.SingleW = _singleW;
            _owner.SingleH = _singleH;

            mFrames = new List<RectangleF[,]>
            {
                new RectangleF[_singleH, _singleW]
            };
            mNavigator = new FrameCollectionNavigator(this);
            Owner = _owner;
        }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_owner"></param>
        protected FrameCollection(Template _owner)
        {
            Owner = _owner;
            mNavigator = new FrameCollectionNavigator(this);

            if (_owner.LeftH != -1 && _owner.LeftW != -1 && _owner.RightH != -1 && _owner.RightW != -1)
            {
                IsSingle = false;
                mFrames = new List<RectangleF[,]>
                {
                    new RectangleF[_owner.LeftH, _owner.LeftW ],
                    new RectangleF[_owner.RightH, _owner.RightW]
                };
            }
            if (_owner.SingleH != -1 && _owner.SingleW != -1)
            {
                IsSingle = true;
                mFrames = new List<RectangleF[,]>
                {
                    new RectangleF[_owner.SingleH, _owner.SingleW ],
                };
            }
        }
        // Временные поля только для передачи параметров
        protected List<TextFrame> mPinnedTextHelperRef = null;
        protected ImageFrame mImageFrameForPinnedTextHelperRef = null;
        /// <summary>
        /// Добавление фрейма в шаблон. Метод автоматически создает соответствующий графический объект <see cref="ImageFrame"/>, и прекрепляет к нему переданные текстовые фрагменты 
        /// Если параметр _shiftCalculator определен
        /// </summary>
        /// <param name="_rect"></param>
        /// <param name="_pinnedText"></param>
        /// <param name="_shiftCalculator">Если параметр определен, то границы объекта <see cref="TextFrame"/> считаются относительно границ <see cref="ImageFrame"/>, к которому привязан текст. Вектор смещения между объектами определеяется данным делегатом</param>
        /// <returns></returns>
        public ImageFrame AddLayoutSegment(RectangleF _rect, Func<TextFrame, PointF> _shiftCalculator, params TextFrame[] _pinnedText)
        {
            if (_pinnedText != null && _pinnedText.Length > 0)
            {
                if (Owner.Texts == null)
                {
                    Owner.Texts = new TextCollection(Owner);
                }
                mPinnedTextHelperRef = new List<TextFrame>();
                foreach (var frame in _pinnedText)
                {
                    if (_shiftCalculator != null)
                    {
                        PointF shiftvector = _shiftCalculator(frame);
                        frame.Bounds = new RectangleF(_rect.X + frame.Bounds.X + shiftvector.X, _rect.Y + frame.Bounds.Y + shiftvector.Y, frame.Bounds.Width, frame.Bounds.Height);
                    }
                    Owner.Texts.Add(frame);
                    mPinnedTextHelperRef.Add(frame);
                }
            }
            mImageFrameForPinnedTextHelperRef = null;
            // Метод Add передаем коллекцию mPinnedTextHelperRef в обработчик событий onFrameAdded. В результате к автоматически созданному объекту ImageFrame будут привязаны ссылки на объекты внутри mPinnedTextHelperRef
            // Внимание, это происходит только при вызове данного метода AddLayoutSegment. При явном вызове метода Add привязки не произойдет
            Add(_rect);
            // После выполнени, в переменной mImageFrameForPinnedTextHelperRef вернется ссылка на созданный экземпляр ImageFrame
            mPinnedTextHelperRef = null;

            return mImageFrameForPinnedTextHelperRef;
        }
        /// <summary>
        /// Абстрактный метод добавление фрейм в шаблон. Автоматически создает соответствующий графический объект <see cref="ImageFrame"/>
        /// </summary>
        /// <param name="_rect"></param>
        /// <returns></returns>
        public abstract bool Add(RectangleF _rect);
        /// <summary>
        /// Абстрактный метод перевода указателя внутри контейнера на указанную сторону шаблона
        /// </summary>
        /// <param name="_side"></param>
        public abstract void SetSide(PageSide _side);
        /// <summary>
        /// Удаляет все элементы контейнера
        /// </summary>
        public virtual void Clear()
        {
            if (mFrames == null) return;

            foreach (var frame in mFrames)
            {
                for (int i = 0; i < frame.GetLength(0); i++)
                {
                    for (int j = 0; j < frame.GetLength(1); j++)
                    {
                        frame[i, j] = default;
                    }
                }
            }
        }
        public IEnumerator<RectangleF> GetEnumerator()
        {
            for (int i = 0; i < mFrames.Count; i++)
            {
                var frame = mFrames[i];
                for (int k = 0; k < frame.GetLength(0); k++)
                {
                    for (int m = 0; m < frame.GetLength(1); m++)
                    {
                        if (frame[k, m] != default) yield return frame[k, m];
                    }
                }
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            for (int i = 0; i < mFrames.Count; i++)
            {
                var frame = mFrames[i];
                for (int k = 0; k < frame.GetLength(0); k++)
                {
                    for (int m = 0; m < frame.GetLength(1); m++)
                    {
                        yield return frame[k, m];
                    }
                }
            }
        }
        /// <summary>
        /// Двумерный массив элементов шаблона на выбранной стороне. Если значение элемента массива принимает default, это означает, что на данной позиции изображения нет
        /// </summary>
        /// <param name="_side"></param>
        /// <returns></returns>
        public RectangleF[,] this[PageSide _side]
        {
            get
            {
                if (_side == PageSide.Single && !IsSingle) return new RectangleF[0, 0];

                switch (_side)
                {
                    case PageSide.Single:
                    case PageSide.Left:
                        return mFrames[0];
                    case PageSide.Right:
                        return mFrames[1];
                    default:
                        return new RectangleF[0, 0];
                }
            }
        }
        /// <summary>
        /// Двумерный массив элементов шаблона на выбранной стороне. Если значение элемента массива принимает default, это означает, что на данной позиции изображения нет
        /// </summary>
        /// <param name="_side"></param>
        /// <returns></returns>
        public RectangleF[,] this[int _index]
        {
            get
            {
                if (_index >= mFrames.Count) return new RectangleF[0, 0];
                return mFrames[_index];
            }
        }
    }

}
