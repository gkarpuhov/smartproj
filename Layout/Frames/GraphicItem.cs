using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Контейнер графических элементов
    /// </summary>
    public class GraphicsCollection : IEnumerable<GraphicItem>
    {
        private List<GraphicItem> mItems;
        /// <summary>
        /// Ссылка на контейнер <see cref="Template"/>
        /// </summary>
        public Template Owner { get; }
        /// <summary>
        /// Объект логирования. Ссылка на экземпляр контейнера <see cref="Template.Log"/>
        /// </summary>
        public Logger Log => Owner?.Log;
        public GraphicItem this[int _index] => mItems[_index];
        public GraphicItem this[string _index] => mItems.Find(x => x.KeyId == _index);
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_owner">Ссылка на объект <see cref="Template"/>, содержащий коллекцию</param>
        public GraphicsCollection(Template _owner)
        {
            Owner = _owner;
            mItems = new List<GraphicItem>();   
        }
        /// <summary>
        /// Добавляет элемент коллекции и привязывает к текущей коллекции через свойство <see cref="GraphicItem.Owner"/>
        /// </summary>
        /// <param name="_item"></param>
        /// <returns></returns>
        public GraphicItem Add(GraphicItem _item)
        {
            mItems.Add(_item);
            _item.Owner = this;
            return _item;
        }
        public void Clear()
        {
            foreach (var item in mItems)
            {
                item.Owner = null;
            }
            mItems.Clear();
        }
        public IEnumerator<GraphicItem> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }
    /// <summary>
    /// Абстрактный класс для представления графического элемента документа. Наследует <see cref="Tree"/>
    /// </summary>
    public abstract class GraphicItem : Tree
    {
        /// <summary>
        /// Ссылка на контейнер <see cref="GraphicsCollection"/>
        /// </summary>
        public GraphicsCollection Owner { get; set; }
        /// <summary>
        /// Объект логирования. Переопределен <see cref="Tree.Log"/>. Ссылка на экземпляр контейнера <see cref="GraphicsCollection.Log"/>
        /// </summary>
        public override Logger Log => Owner?.Log;
        /// <summary>
        /// Возвращает 'Истина' если цвет заливки имеет отличную от нуля степеь прозрачности и объект имеет определенную область
        /// </summary>
        public bool HasFill => FillColor.A > 0 && Bounds != default;
        /// <summary>
        /// Возвращает 'Истина' если цвет обводки имеет отличную от нуля степеь прозрачности и толщину линии
        /// </summary>
        public bool HasStroke => StrokeColor.A > 0 && StrokeWeight > 0;
        /// <summary>
        /// Индекс слоя, на котором расположен объект. По умолчанию в большинстве случаяю каждый тип графического объекта будет расположен на определенном слое:
        /// 0 - нижний слой стачичное изображения, фон, паттерн (ImageItem)
        /// 1 - фрейм с изображением (ImageFrame)
        /// 2 - графические объект-заливка, прямоугольник или эллипс (FillItem, EllipseItem)
        /// 3 - текстовый фрейм (TextFrame)
        /// ? - ClipItem пока не определен порядок работы.
        /// Доступно для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public int Layer { get; set; }
        /// <summary>
        /// Область рисования графического объекта. Доступно для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public RectangleF Bounds { get; set; }
        /// <summary>
        /// Цвет области заливки графического объекта. Доступно для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public Color FillColor { get; set; }
        /// <summary>
        /// Цвет линии обводки графического объекта. Доступно для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public Color StrokeColor { get; set; }
        /// <summary>
        /// Толщина линии обводки графического объекта. Доступно для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public float StrokeWeight { get; set; }
        /// <summary>
        /// Абстрактный тип графического объекта
        /// </summary>
        public abstract GraphicTypeEnum GraphicType { get; }
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        protected GraphicItem() : base()
        {
        }
    }
    /// <summary>
    /// Прямоугольная область заливки. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class FillItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>. Доступен для сериализации <see cref="Serializer"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Fill"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Fill;
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public FillItem() : base()
        {
            Layer = 2;
        }
    }
    /// <summary>
    /// Путь отсечки. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class ClipItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>. Доступен для сериализации <see cref="Serializer"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Clip"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Clip;
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public ClipItem() : base()
        {
        }
    }
    /// <summary>
    /// Круглая область заливки. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class EllipseItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>. Доступен для сериализации <see cref="Serializer"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Ellipse"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Ellipse;
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public EllipseItem() : base()
        {
            Layer = 2;
        }
    }
    /// <summary>
    /// Растровый графический объект. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class ImageItem : GraphicItem
    {
        /// <summary>
        /// Идентификатор изображения в связанном объекте документа <see cref="GdPicture14.GdPictureImaging"/>
        /// </summary>
        public int ImageID { get; set; }
        /// <summary>
        /// Идентификатор изображения в связанном объекте PDF <see cref="GdPicture14.GdPicturePDF"/> документа <see cref="Job.PdfObject"/>
        /// </summary>
        public string FdfID { get; set; }
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>. Доступен для сериализации <see cref="Serializer"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Image"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Image;
        /// <summary>
        /// Имя файла изображения, помещаемого в данный графический объект. Доступен для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public string FileName { get; set; }
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public ImageItem() : base()
        {
            Layer = 0;
        }
    }
    /// <summary>
    /// Растровый графический объект представляющий собой фрейм для автоматической подстановки изображения по выбранному алгоритму. Наследовано <see cref="ImageItem"/>
    /// </summary>
    public class ImageFrame : ImageItem
    {
        /// <summary>
        /// Идентификатор ссылка на коллекция объектов параметров файла, который будет вставлен в фрейм
        /// </summary>
        public int ProcessID { get; set; }
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>. Доступен для сериализации <see cref="Serializer"/>.
        /// Имеет значение <see cref="GraphicTypeEnum.ImageFrame"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.ImageFrame;
        /// <summary>
        /// Координаты соответствия данного графического объекта определенному фрейму в коллекции шаблона <see cref="FrameCollection"/>. Доступен для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public Point FrameID { get; set; }
        /// <summary>
        /// Сторона расположения в шаблоне, содержащая данный объект. Доступен для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public PageSide FrameSide { get; set; }
        /// <summary>
        /// Тип формы данного объекта фрейма. В соответствии с данным свойством на изображение будет наложены маска соответствующей формы. Доступен для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlElement]
        public ImageFrameShapeEnum FrameShape { get; set; }
        /// <summary>
        /// Ссылка на экземпляр контейнера, содержащего ссылки на текстовуй информацию, прикрепленную к данному графическому объекту. Доступен для сериализации <see cref="Serializer"/>
        /// </summary>
        [XmlCollection(false, false, typeof(Guid), typeof(ImageFrame))]
        public PinnedTextCollection PinnedText { get; set; }
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public ImageFrame() : base()
        {
            Layer = 1;
            FrameShape = ImageFrameShapeEnum.Rectangle;
        }
        /// <summary>
        /// Контейнер, содержащий ссылки на текстовуй информацию, прикрепленную к данному графическому объекту
        /// </summary>
        public class PinnedTextCollection : IEnumerable<Guid>
        {
            private List<Guid> mItems;
            private ImageFrame mOwner;
            /// <summary>
            /// Добавляет в контейнер ссылки на все объекты определенного текстового сегмента
            /// </summary>
            /// <param name="_frame"></param>
            public void AddFrame(TextFrame _frame)
            {
                if (_frame != null)
                {
                    Add(_frame.UID);
                }
            }
            /// <summary>
            /// Добавляет ссылку на объект текстовой строки. Если в коллекции шаблона <see cref="TextCollection"/> содержится объект с соответствующим идентификатором, он станет привязанным к данной коллекции
            /// </summary>
            /// <param name="_item"></param>
            public void Add(Guid _item)
            {
                TextFrame textFrame = mOwner?.Owner?.Owner.Texts?[_item];
                if (!mItems.Contains(_item))
                {
                    mItems.Add(_item);
                }
                if (textFrame != null)
                {
                    // Свойство 'PinObject' проверяет наличие элемента в коллекции. Если его нет, то вызывет внутри этот же метод, если это возможно. 
                    // В данном случае вызова не произойдет, так как мы уже физически добалили элемент в коллекцию
                    textFrame.PinObject = mOwner;
                }
            }
            /// <summary>
            /// Удаляет все элементы коллекции
            /// </summary>
            public void Clear() => mItems.Clear();
            /// <summary>
            /// Удаляет элемент коллекции
            /// </summary>
            /// <param name="_item"></param>
            /// <returns></returns>
            public bool Remove(Guid _item) => mItems.Remove(_item);
            /// <summary>
            /// Проверяет наличие определенного элемента в коллекции
            /// </summary>
            /// <param name="_item"></param>
            /// <returns></returns>
            public bool Contains(Guid _item) => mItems.Contains(_item);
            public IEnumerator<Guid> GetEnumerator()
            {
                return mItems.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return mItems.GetEnumerator();
            }
            /// <summary>
            /// Констркутор по умолчанию
            /// </summary>
            public PinnedTextCollection(ImageFrame _owner) 
            {
                mItems = new List<Guid>();
                mOwner = _owner;
            }
        }

    }
}
