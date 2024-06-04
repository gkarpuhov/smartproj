using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Класс для описание коллекции доступных шрифтов
    /// </summary>
    public class FontCollection : IEnumerable<FontClass>
    {
        private List<FontClass> mItems;
        public FontClass this[int _uid] => mItems.Find(x => x.GetHashCode() == _uid);
        public IEnumerable<FontClass> this[string _name] => mItems.Where(x => String.Compare(x.Family, _name, true) == 0);
        public int Count => mItems.Count;
        public WorkSpace Owner { get; }
        public FontCollection(WorkSpace _owner) 
        {
            Owner = _owner;
            mItems = new List<FontClass>();
        }
        public FontClass Add(FontClass _item)
        {
            if (_item != null && !Contains(_item))
            {
                _item.Owner = this;
                mItems.Add(_item);
            }
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
        public bool Contains(FontClass _class)
        {
            if (_class == null) return false;
            return mItems.Any(x => x.Id == _class.Id);
        }
        public IEnumerator<FontClass> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }
    /// <summary>
    /// Класс для описания параметров шрифта. Класс не содержит в явном виде содержимого шрифта, только его свойства. Используется только для удобства передачи параметров шрифта, и их сериализации
    /// Доступные для использования шрифты определяются ИСКЛЮЧИТЕЛЬНО в системных папках, определяемых статическим классом GdPictureDocumentUtilities. Другие источники на данный момент не задействуются
    /// Обеспечивает механизм связи текстовых объектов через внутрениий Id непосредственно с физическим шрифтом
    /// Проверить работоспособность шрифта на данном этапе мы не можем. Возможные ошибки будут выявлены только на этапе работы с объектом PDF файла
    /// </summary>
    public class FontClass
    {
        /// <summary>
        /// Уникальный идентификатор. Используется для сериализации данных
        /// </summary>
        [XmlElement]
        public int Id => GetHashCode();
        /// <summary>
        /// Стиль шрифта. Использует стандартный тип <see cref="FontStyle"/>
        /// </summary>
        [XmlElement]
        public FontStyle Style { get; set; }
        /// <summary>
        /// Идентификатор семейства шрифта
        /// </summary>
        [XmlElement]
        public string Family { get; set; }
        /// <summary>
        /// Описание назначения данного шрифта
        /// </summary>
        [XmlElement]
        public string Description { get; set; }
        /// <summary>
        /// Полужирный стиль
        /// </summary>
        public bool IsBold => (Style & FontStyle.Bold) == FontStyle.Bold;
        /// <summary>
        /// Курсив
        /// </summary>
        public bool IsItalic => (Style & FontStyle.Italic) == FontStyle.Italic;
        public FontCollection Owner { get; set; }
        /// <summary>
        /// Конструктор по умолчанию ограниченной доступности для использования при десериализации
        /// </summary>
        protected FontClass()
        {
        }
        /// <summary>
        /// Конструктор по умолчанию с передачей параметров для использования в коде
        /// </summary>
        /// <param name="_name"></param>
        /// <param name="_style"></param>
        /// <param name="_description"></param>
        public FontClass(string _name, FontStyle _style, string _description)
        {
            Style = _style;
            Family = _name;
            Description = _description;
        }
        /// <summary>
        /// Уникальный идентификатор шрифта, используемый для сериализации свойств текста
        /// Переопределенный (вместо стандартного) статический класс-расширение хеш-суммы <see cref="StringEx.StringHashCode40"/> используется для используется для гарантии неизменности, независимо от хотелок системы
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return Family.StringHashCode40() ^ Style.ToString().StringHashCode40();
        }
    }
}
