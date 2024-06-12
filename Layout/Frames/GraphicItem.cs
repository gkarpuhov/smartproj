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
        public int Count => mItems.Count;
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
        /// Аттрибут, определяющий что графический элемент попадает на корешок разворотного шаблона, и должен быть добавлен в макет дважды на разные страницы
        /// Не определен изначально. Свойство используется контроллерами, которые логику могут определять по своему усмотрению
        /// </summary>
        public bool ExtraBounds { get; set; }
        /// <summary>
        /// Возвращает 'Истина' если цвет заливки имеет отличную от нуля степеь прозрачности и объект имеет определенную область
        /// </summary>
        public bool HasFill => FillColor.A > 0 && Bounds != default;
        /// <summary>
        /// Возвращает 'Истина' если цвет обводки имеет отличную от нуля степеь прозрачности и толщину линии
        /// </summary>
        public bool HasStroke => StrokeColor.A > 0 && Bounds != default && StrokeWeight > 0;
        /// <summary>
        /// Пример распределения пр слоям
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
        /// Сторона расположения в шаблоне, содержащая данный объект
        /// </summary>
        [XmlElement]
        public PageSide FrameSide { get; set; }
        /// <summary>
        /// Область рисования графического объекта
        /// </summary>
        [XmlElement]
        public RectangleF Bounds { get; set; }
        /// <summary>
        /// Цвет области заливки графического объекта
        /// </summary>
        [XmlElement]
        public Color FillColor { get; set; }
        /// <summary>
        /// Цвет линии обводки графического объекта
        /// </summary>
        [XmlElement]
        public Color StrokeColor { get; set; }
        /// <summary>
        /// Толщина линии обводки графического объекта
        /// </summary>
        [XmlElement]
        public float StrokeWeight { get; set; }
        /// <summary>
        /// Матрица геометрической трансформации
        /// </summary>
        [XmlElement]
        public float[] TransformationMatrix { get; set; }
        /// <summary>
        /// Абстрактный тип графического объекта
        /// </summary>
        public abstract GraphicTypeEnum GraphicType { get; }
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        protected GraphicItem() : base()
        {
            ExtraBounds = false;
            TransformationMatrix = new float[6] { 0, 0, 0, 0, 0, 0, };
        }
    }
    /// <summary>
    /// Прямоугольная область заливки. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class FillItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Fill"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Fill;
        /// <summary>
        /// Тип формы данного объекта фрейма
        /// </summary>
        [XmlElement]
        public ImageFrameShapeEnum FrameShape { get; set; }
        /// <summary>
        /// Радиус скругления угла для объекта типа <see cref="ImageFrameShapeEnum.Rounded"/>
        /// </summary>
        [XmlElement]
        public float Radius { get; set; }
        /// <summary>
        /// Констркутор по умолчанию
        /// </summary>
        public FillItem() : base()
        {
            Layer = 2;
            FrameShape = ImageFrameShapeEnum.Rectangle;
            Radius = 3;
        }
    }
    /// <summary>
    /// Путь отсечки. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class ClipItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>
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
    /// Растровый графический объект. Наследовано <see cref="GraphicItem"/>
    /// </summary>
    public class ImageItem : GraphicItem
    {
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>
        /// Имеет значение <see cref="GraphicTypeEnum.Image"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.Image;
        /// <summary>
        /// Имя файла изображения, помещаемого в данный графический объект
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
        /// Настройка по умолчанию величины безопасного отступа внуть фрейма, в пределах которого не должно находиться значимых элементов изображения
        /// </summary>
        [XmlElement]
        public float SafeFrameZone { get; set; }
        /// <summary>
        /// Отступы со всех сторон в пределах обрезного формата шаблона (если не применятся специальная логика), определяющие зону для автоматического позиционирования выбранной части изображения
        /// </summary>
        [XmlElement]
        public Margins AutoFitMargins { get; set; }
        /// <summary>
        /// Флаг определяющий, каким образом выбранный участок изображения должен быть позиционированн в области, ограниченной зоной AutoFitMargins
        /// </summary>
        [XmlElement]
        public PositionEnum AutoFitPaddling { get; set; }
        /// <summary>
        /// Флаг определяет, какой именно тип объекта или группы объектов должны быть использованы для автоматического позиционирования.
        /// 1: Если значение равно <see cref="AutoPositionObjectTypeEnum.Off"/>, попытки автоматического позиционирования не происходит
        /// 2: <see cref="AutoPositionObjectTypeEnum.OneFace"/> - Позиционирование будет выполнено только если на изображении найдено одно лицо. Изображение будет масштабированеие такои образом, чтобы размеры лица привести к значению AutoDetectAndFocusWidth, либо во всю зону, ограниченную AutoFitMargins. Затем происходти позиционирование в соответствии с флагом AutoFitPaddling.
        /// 3: <see cref="AutoPositionObjectTypeEnum.GroupFaces"/> - Применяется если на изображении найдено более одно лица. Изображение будет масштабированеие такои образом, чтобы размеры области группы лиц привести к зоне, ограниченной AutoFitMargins. Затем происходти позиционирование в соответствии с флагом AutoFitPaddling.
        /// При обработке групповых фотография логика масштабирования не предусматриваем увеличение изображения до границ области! Только уменьшение при необходимости и возможености.
        /// Проверка попадания лиц на корешок/биговку не предусмотрена.
        /// 4: <see cref="AutoPositionObjectTypeEnum.ProtectFaces"/> - Масштабирование не происходит! Просходит попытка позиционировать изображение таким образом, чтобы области лиц не выходили за пределы зоны, ограниченной AutoFitMargins. При этом отступы AutoFitMargins в данном случае определены не только от обрезного формата, но и от позиции корешка/биговки (центра разворота), другими словами - от обрезного форматы полосы.
        /// Если условие для позиционирования выполнить невозможно, вставка изображения запрещена. В зависимости от логики контроллера происходит, либо критическая ошибка, либо подбирается другое изображение
        /// </summary>
        [XmlElement]
        public AutoPositionObjectTypeEnum AutoFitObjectType { get; set; }
        /// <summary>
        /// Точная ширина объекта в мм, который должен быть автоматически приведен к данному размеру. 
        /// Изображение должно быть пропорционально масштабировано таким образом, чтобы горизонтальный размер выделенной области (ширина лица, например) стал равен данной величине.
        /// Если значение равно 0, масштабирование изменит размер изображения таким образом, чтобы лицо было помещено во всю зону, определенную границами AutoFitMargins
        /// </summary>
        [XmlElement]
        public float AutoDetectAndFocusWidth { get; set; }
        /// <summary>
        /// Идентификатор ссылка на коллекцию объектов параметров файла, который будет вставлен в фрейм
        /// </summary>
        //public int ProcessID { get; set; }
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>
        /// Имеет значение <see cref="GraphicTypeEnum.ImageFrame"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType { get; } = GraphicTypeEnum.ImageFrame;
        /// <summary>
        /// Координаты соответствия данного графического объекта определенному фрейму в коллекции шаблона <see cref="FrameCollection"/>
        /// </summary>
        [XmlElement]
        public Point FrameID { get; set; }
        /// <summary>
        /// Тип формы данного объекта фрейма. В соответствии с данным свойством на изображение будет наложены маска соответствующей формы
        /// </summary>
        [XmlElement]
        public ImageFrameShapeEnum FrameShape { get; set; }
        /// <summary>
        /// Радиус скругления угла для объекта типа <see cref="ImageFrameShapeEnum.Rounded"/>
        /// </summary>
        [XmlElement]
        public float Radius { get; set; }
        /// <summary>
        /// Ссылка на экземпляр контейнера, содержащего ссылки на текстовуй информацию, прикрепленную к данному графическому объекту
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
            Radius = 3;
            AutoFitObjectType = AutoPositionObjectTypeEnum.Off;
            AutoFitPaddling = PositionEnum.TopCenter;
            AutoFitMargins = new Margins(5, 5, 5, 5);
            SafeFrameZone = 3;
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
