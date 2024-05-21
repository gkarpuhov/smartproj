using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
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
        bool Start(object[] _settings);
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
        public abstract bool Start(object[] _settings);
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
