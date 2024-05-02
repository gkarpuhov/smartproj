using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Smartproj
{
    public enum ControllerStatusEnum
    {
        New = 0,
        Activated,
        Processing,
        Error,
        Finished,
        Disposed
    }
    /// <summary>
    /// Общий интерфейс для любых обработчиков данных
    /// </summary>
    public interface IController : IDisposable
    {
        ControllerColelction Owner { get; set; }
        int Priority { get; set; }
        bool Enabled { get; set; }
        Guid UID { get; }
        ControllerStatusEnum StatusEnum { get; }
        bool Activate(params object[] _settings);
        void Start();
    }
    /// <summary>
    /// Абстрактная реализация обработчика данных
    /// </summary>
    public abstract class AbstractController : IController
    {
        [XmlElement]
        public Guid UID { get; protected set; }
        /// <summary>
        /// Свойство определяет порядок в синхронной общей очереди на обработку
        /// </summary>
        [XmlElement]
        public int Priority { get; set; }
        /// <summary>
        /// Если свойство имеет значение false, то при вызове метода <see cref="Start"/> никаие действия не будут выполнены
        /// При этом метод <see cref="Activate"/> в любом случае должен вернуть успешный результат
        /// </summary>
        [XmlElement]
        public bool Enabled { get; set; }
        public ControllerColelction Owner { get; set; }
        /// <summary>
        /// Текущий статус выполнения процессов контроллером
        /// </summary>
        public ControllerStatusEnum StatusEnum { get; protected set; }
        public Logger Log => Owner?.Log;
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        protected AbstractController()
        {
            StatusEnum = ControllerStatusEnum.New;
            UID = Guid.NewGuid();
        }
        /// <summary>
        /// Активирует контроллер в состояние готовности к выполнению работы
        /// </summary>
        /// <param name="_settings"></param>
        public virtual bool Activate(params object[] _settings)
        {
            Job job = Owner?.Owner?.Owner;
            if (job == null)
            {
                Log?.WriteError("AbstractController.Activate", "Ошибка при активации контроллера создания PDF файла. Для активации контроллер должен быть добавлен в систему");
                StatusEnum = ControllerStatusEnum.Error;
                return false;
            }
            return true;
        }
        /// <summary>
        /// Выполнения определенного для данного контроллера, процесса
        /// </summary>
        public abstract void Start();
        protected virtual void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (StatusEnum == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            }
            StatusEnum = ControllerStatusEnum.Disposed;
        }
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
        /// <summary>
        /// Строка, определеющая путь источника получения данных
        /// </summary>
        string Source { get; }
    }
    public class ControllerColelction : IEnumerable<IController>
    {
        private List<IController> mItems;
        public int Count => mItems.Count;
        public Logger Log => Owner?.Log;
        public Product Owner { get; }
        public IController this[Guid _uid] => mItems.Find(x => x.UID == _uid);
        public ControllerColelction(Product _owner) 
        {
            mItems = new List<IController>();
            Owner = _owner;
        }
        public IController Add(IController _controller)
        {
            if (_controller != null)
            {
                _controller.Owner = this;
                mItems.Add(_controller);
            }
            return _controller;
        }
        public void Clear()
        {
            foreach (var item in mItems)
            {
                item.Owner = null;
            }
            mItems.Clear();
        }
        public IEnumerator<IController> GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mItems.GetEnumerator();
        }
    }
}
