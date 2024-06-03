using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Color = System.Drawing.Color;

namespace Smartproj
{
    /// <summary>
    /// Структура, определяющая относительное смешение текстового фрейма относительно связанного изображения.
    /// Точка отсчета от фрейма изображение - стандартное (левый нижний угол)
    /// </summary>
    public class RelativePosition
    {
        [XmlElement]
        public PointF Shift { get; set; }
        [XmlElement]
        public PositionEnum LinkTo { get; set; }
        public RelativePosition()
        {
            LinkTo = PositionEnum.TopLeft;
        }
    }
    /// <summary>
    /// Структура для передачи параметров свойств форматирования текста
    /// </summary>
    public struct TextParameters
    {
        public float Size;
        public FontClass Font;
        public Color FillColor;
        public Color StrokeColor;
        public float StrokeWeight;
        public int Position;
        public HorizontalPositionEnum Paddling;
    }
    /// <summary>
    /// Структура, описывающая свойства и параметры одного символа
    /// </summary>
    public struct Glyph
    {
        public byte Code;
        public float Size;
        public int Font;
        public int Line;
        public int Paragraph;
        public UInt32 FillColor;
        public UInt32 StrokeColor;
        public float StrokeWeight;
    }
    /// <summary>
    /// Коллекция всех текстовых фрагментов шаблона. Наследует <see cref="GraphicsCollection"/>
    /// </summary>
    public class TextCollection : GraphicsCollection
    {
        /// <summary>
        /// Представление объекта в виде одной строки без индивидуальных параметров каждого символа
        /// </summary>
        public string Value
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (TextFrame item in this)
                {
                    sb.Append(item.Value);
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Представление объекта в виде набора слов строкового типа, без индивидуальных параметров каждого символа.
        /// Разделителями слов принимаем пробелы и знаки препинания
        /// </summary>
        public IEnumerable<string> Words
        {
            get
            {
                List<string> words = new List<string>();
                foreach (TextFrame item in this)
                {
                    words.AddRange(item.Words);
                }
                return words;
            }
        }
        /// <summary>
        /// Представление объекта в виде коллекции строк без индивидуальных параметров каждого символа
        /// </summary>
        public IEnumerable<string> Lines
        {
            get
            {
                List<string> lines = new List<string>();
                foreach (TextFrame item in this)
                {
                    lines.AddRange(item.Lines);
                }
                return lines;
            }
        }
        public TextFrame this[Guid _id] => (TextFrame)this.SingleOrDefault(x => ((TextFrame)x).UID == _id);
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_owner">Ссылка на объект <see cref="Template"/>, содержащий коллекцию</param>
        public TextCollection(Template _owner) : base(_owner) 
        {
        }
        /// <summary>
        /// Добавляет новый текстовый сегмент содержащий определенный текст с определенными параметрами форматирования
        /// </summary>
        /// <param name="_bounds"></param>
        /// <param name="_value"></param>
        /// <param name="_position"></param>
        /// <param name="_parameters"></param>
        /// <param name="_key"></param>
        /// <returns></returns>
        public TextFrame AddTextFrame(RectangleF _bounds, string _value, VerticalPositionEnum _position, TextParameters _parameters, string _key = null)
        {
            TextFrame textFrame = new TextFrame() { Bounds = _bounds, Position = _position, KeyId = _key };
            Add(textFrame);

            if (_value != null && _value != "")
            {
                textFrame.AddLines(_value, _parameters);
            }

            return textFrame;
        }
    }
    /// <summary>
    /// Тестовый фрагмент данных. Наследует <see cref="GraphicItem"/>
    /// </summary>
    public class TextFrame : GraphicItem
    {
        private Guid mUID;
        private ImageFrame mPinObject;
        /// <summary>
        /// Ссылка на объект <see cref="ImageFrame"/>, к которому может быть прикреплен данный текстовый фрейм
        /// </summary>
        public ImageFrame PinObject
        {
            get { return mPinObject; }
            set
            {
                if (value != null && mPinObject != value)
                {
                    if (mPinObject?.PinnedText != null && mPinObject.PinnedText.Contains(UID))
                    {
                        mPinObject.PinnedText.Remove(UID);
                    }
                    if (value.PinnedText != null && !value.PinnedText.Contains(UID))
                    {
                        value.PinnedText.Add(UID);
                    }
                    mPinObject = value;
                }
            }
        }
        /// <summary>
        /// Коллекция объектов строк <see cref="TextLine"/>, принадлежащих данному текстовому фрагменту
        /// </summary>
        public IEnumerable<TextLine> TextLines => TreeNodeItems.OfType<TextLine>();
        public bool IsEmpty => !TextLines.Any(x => x.Glyphs.Count > 0);
        /// <summary>
        /// Представление объекта в виде одной строки без индивидуальных параметров каждого символа
        /// </summary>
        public string Value
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (TextLine item in TreeNodeItems)
                {
                    sb.AppendLine(item.Value);
                }
                return sb.ToString();
            }
        }
        /// <summary>
        /// Представление объекта в виде набора слов строкового типа, без индивидуальных параметров каждого символа.
        /// Разделителями слов принимаем пробелы и знаки препинания
        /// </summary>
        public IEnumerable<string> Words
        {
            get
            {
                List<string> words = new List<string>();
                foreach (TextLine item in TreeNodeItems)
                {
                    words.AddRange(item.Words);
                }
                return words;
            }
        }
        /// <summary>
        /// Представление объекта в виде коллекции строк без индивидуальных параметров каждого символа
        /// </summary>
        public IEnumerable<string> Lines => TreeNodeItems.Select(x => ((TextLine)x).Value);
        /// <summary>
        /// Уникальный идентификатора объекта. Предназначен для обеспечения механизма ссылочной связи с объектом <see cref="ImageFrame"/>.
        /// Если в момент десериализации в системе будет найден объект <see cref="ImageFrame"/> с ссылкой на данный Guid, то он будет привязан к данному тексту
        /// </summary>
        [XmlElement]
        public Guid UID
        {
            get { return mUID; }
            set
            {
                ImageFrame imageframe = (ImageFrame)Owner?.Owner?.Graphics?.SingleOrDefault(x => x.GraphicType == GraphicTypeEnum.ImageFrame && ((ImageFrame)x).PinnedText != null && ((ImageFrame)x).PinnedText.Contains(value));
                if (imageframe != null)
                {
                    mPinObject = imageframe;
                }
                mUID = value;
            }
        }
        [XmlContainer]
        public RelativePosition PositionToImage { get; set; }
        /// <summary>
        /// Тип данной реализации родительского класса <see cref="GraphicItem"/>
        /// Имеет значение <see cref="GraphicTypeEnum.TextFrame"/>
        /// </summary>
        [XmlElement]
        public override GraphicTypeEnum GraphicType => GraphicTypeEnum.TextFrame;
        /// <summary>
        /// Текст данного объекта доступен только для чтения
        /// </summary>
        [XmlElement]
        public bool ReadOnly { get; set; }
        /// <summary>
        /// Значение интервала между строками объектов <see cref="TextLine"/>
        /// </summary>
        [XmlElement]
        public float Interval { get; set; }
        /// <summary>
        /// Выравнивание по вертикали текстовой информации объекта
        /// </summary>
        [XmlElement]
        public VerticalPositionEnum Position { get; set; }
        /// <summary>
        /// Отступ от внешних границ текстового сегмента до текста
        /// </summary>
        [XmlElement]
        public float Space { get; set; }
        /// <summary>
        /// Конструктор по умолчанию. Устанавливает Значение <see cref="GraphicItem.Layer"/> равным 3.
        /// </summary>
        public TextFrame() : base()
        {
            mUID = Guid.NewGuid();
            Layer = 3;
        }
        /// <summary>
        /// Открепление данного фрейма от связанного объекта <see cref="ImageFrame"/>
        /// </summary>
        public void UnPin()
        {
            if (mPinObject != null && mPinObject.PinnedText != null)
            {
                mPinObject.PinnedText.Remove(UID);
            }
            mPinObject = null;
        }
        /// <summary>
        /// Добавляет объект для новой строки в коллекцию узлов текущего объекта, используя параметры <see cref="TextParameters"/>
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_parameters"></param>
        /// <param name="_pos"></param>
        /// <param name="_key"></param>
        /// <returns></returns>
        public TextLine AddLine(string _value, TextParameters _parameters, int _pos = -1, string _key = null)
        {
            if (_value == null) return null;
            TextLine textLine = new TextLine() { Position = _parameters.Paddling, KeyId = _key };
            Insert(_pos, textLine);
            textLine.AddString(_value, _parameters);
            return textLine;
        }
        public void AddLines(IEnumerable<string> _values, TextParameters _parameters, Func<string> _keySelector = null)
        {
            if (_values == null) return;
            foreach (var line in _values)
            {
                AddLine(line, _parameters, -1, _keySelector != null ? _keySelector() : null);
            }
        }
        public void AddLines(string _values, TextParameters _parameters, Func<string> _keySelector = null)
        {
            var matches = Regex.Matches(_values, @"([^\r^\n]+)?(\r?\n|$)", RegexOptions.Compiled);
            foreach (Match match in matches)
            {
                if (match.Groups[1].Value != "" || match.Groups[2].Value != "")
                {
                    AddLine(match.Groups[1].Value, _parameters, -1, _keySelector != null ? _keySelector() : null);
                }
            }
        }
    }
    /// <summary>
    /// Описание одной строки в виде набора глифов произвольных параметров (называем просто 'Текстовая строка'). Наследует <see cref="Tree"/>
    /// </summary>
    public class TextLine: Tree
    {
         /// <summary>
        /// Ссылка на родительский объект текстового фрагмента <see cref="TextFrame"/>, содержащий данную строку.
        /// Указывает на свойство <see cref="Tree.Parent"/>, родительского класса <see cref="Tree"/>
        /// </summary>
        public TextFrame Owner => (TextFrame)Parent;

        /// <summary>
        /// Представление объекта в виде строковом виде, без учета индивидуальных параметров каждого символа
        /// </summary>
        public string Value => GetValue(Glyphs);
        /// <summary>
        /// Представление в виде набора слов строкового типа, без индивидуальных параметров каждого символа.
        /// Разделителями слов принимаем пробелы и знаки препинания
        /// </summary>
        public IEnumerable<string> Words => GetWords(Glyphs);
        //public IEnumerable<KeyValuePair<TextParameters, string>> Intervals => GetIntervals(Glyphs);
        /// <summary>
        /// Представление объекта в виде коллекции объектов глифов <see cref="Glyph"/>
        /// Каждый символ имеет набор свойств, определяющих вид текста
        /// </summary>
        internal List<Glyph> Glyphs { get; private set; }
        /// <summary>
        /// Свойство выравнивания строки по горизонтали.
        /// </summary>
        [XmlElement]
        public HorizontalPositionEnum Position { get; set; }
        /// <summary>
        /// Представление объекта набора глифов в виде массива двоичных данных в кодировке Base64.
        /// Свойство предназначено только для серализации данных
        /// </summary>
        [XmlElement]
        protected string Bin
        {
            get
            {
                if (Glyphs != null)
                {
                    return Convert.ToBase64String(Glyphs.ToArray().ToBuffer());
                }
                return null;
            }
            set
            {
                if (value != null && value != "")
                {
                    Glyphs = new List<Glyph>(Convert.FromBase64String(value).FromBuffer<Glyph>());
                }
            }
        }
        static string GetValue(IEnumerable<Glyph> _input)
        {
            return System.Text.Encoding.UTF8.GetString(_input?.Select(x => x.Code).ToArray());
        }
        static IEnumerable<string> GetLines(IEnumerable<Glyph> _input)
        {
            if (_input != null)
            {
                var matches = Regex.Matches(System.Text.Encoding.UTF8.GetString(_input.Select(x => x.Code).ToArray()), @"([^\r^\n]+)?(\r?\n|$)", RegexOptions.Compiled);
                List<string> lines = new List<string>(matches.Count);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value != "") lines.Add(match.Groups[1].Value);
                }
                return lines;
            }
            return null;
        }
        internal static IEnumerable<string> GetWords(IEnumerable<Glyph> _input)
        {
            if (_input != null)
            {
                var matches = Regex.Matches(System.Text.Encoding.UTF8.GetString(_input.Select(x => x.Code).ToArray()), @"([^\r^\n^\s^\.^\,^\;^\:^\""^\']+)?([\.\,\;\:\""\']|\s+|\r?\n|$)", RegexOptions.Compiled);
                List<string> words = new List<string>(matches.Count);
                foreach (Match match in matches)
                {
                    if (match.Groups[1].Value != "") words.Add(match.Groups[1].Value);
                }
                return words;
            }
            return null;
        }
        /// <summary>
        /// Конструктор по умолчанию. Создает пустой объект набора глифов и новый уникальный идентификатора данного объекта
        /// </summary>
        public TextLine() : base()
        {
            Glyphs = new List<Glyph>();
        }
        /// <summary>
        /// Добавляет в объект набор символов с определенными свойствами
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_parameters"></param>
        /// <param name="_pos"></param>
        public void AddString(string _value, TextParameters _parameters, int _pos = -1)
        {
            AddString(_value, _parameters.Size, _parameters.Font.GetHashCode(), _parameters.FillColor, _parameters.StrokeColor, _parameters.StrokeWeight, _pos);
        }
        /// <summary>
        /// Добавляет в объект набор символов с определенными свойствами
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_size"></param>
        /// <param name="_font"></param>
        /// <param name="_fillColor"></param>
        /// <param name="_strokeColor"></param>
        /// <param name="_strokeWeight"></param>
        /// <param name="_pos"></param>
        protected void AddString(string _value, float _size, int _font, Color _fillColor, Color _strokeColor, float _strokeWeight, int _pos = -1)
        {
            var data = Glyphs;
            if (_value != null && _value != "" && data != null)
            {
                IEnumerable<Glyph> newdata = Encoding.UTF8.GetBytes(_value.Replace(Environment.NewLine, "")).Select(x => new Glyph() { Code = x, Size = _size, Font = _font, FillColor = _fillColor.ToUint32(), StrokeColor = _strokeColor.ToUint32(), StrokeWeight = _strokeWeight, Paragraph = Owner.Index, Line = Index });
                if (_pos > data.Count || _pos < 0)
                {
                    data.AddRange(newdata);
                }
                else
                {
                    data.InsertRange(_pos, newdata);
                }
            }
        }
        /// <summary>
        /// Удаляет все данные текущего объекта, и заменяет новыми из заданной строки, используя параметры <see cref="TextParameters"/>
        /// </summary>
        /// <param name="_value"></param>
        /// <param name="_parameters"></param>
        public void Replace(string _value, TextParameters _parameters)
        {
            Glyphs?.Clear();
            AddString(_value, _parameters);
        }
        /// <summary>
        /// Заменяет символы в текущем наборе глифов <see cref="Glyph"/> новыми из заданной строки с сохранеием параметров в соответствии с позицией символов.
        /// Если текущий массив пустой, метод вызывет исключение.
        /// Если новая строка длиннее текущей, к лишнем символам применяются параметры последнего глифа <see cref="Glyph"/>
        /// </summary>
        /// <param name="_value"></param>
        /// <exception cref="InvalidOperationException">Не определен текущий массив данных</exception>
        public void Update(string _value)
        {
            var data = Glyphs;
            if (data != null)
            {
                if (data.Count == 0)
                {
                    //{ AddString(_value, Default); return; }
                    // Метод не применяется, если текущие данные не определены
                    throw new InvalidOperationException();
                }
                if (_value == null || _value == "") { data.Clear(); return; }

                byte[] newdata = Encoding.UTF8.GetBytes(_value.Replace(Environment.NewLine, ""));

                for (int i = 0; i < newdata.Length; i++)
                {
                    if (i < data.Count)
                    {
                        data[i] = new Glyph() { Code = newdata[i], Size = data[i].Size, Font = data[i].Font, FillColor = data[i].FillColor, StrokeColor = data[i].StrokeColor, StrokeWeight = data[i].StrokeWeight, Paragraph = data[i].Paragraph, Line = data[i].Line };
                    }
                    else 
                    {
                        data.Add(new Glyph() { Code = newdata[i], Size = data[i - 1].Size, Font = data[i - 1].Font, FillColor = data[i - 1].FillColor, StrokeColor = data[i - 1].StrokeColor, StrokeWeight = data[i - 1].StrokeWeight, Paragraph = data[i - 1].Paragraph, Line = data[i - 1].Line });
                    }
                }

                if (newdata.Length < data.Count)
                {
                    data.RemoveRange(newdata.Length, data.Count - newdata.Length);
                }
            }
        }
    }
}
