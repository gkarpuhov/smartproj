using Smartproj.Utils;
using System;
using System.Threading;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Абстрактная реализация контроллера, расширенного интерфейсом <see cref="IInputProvider"/>. Определяет способ получения исходных данных и параметров для обработки задания.
    /// Данный тип контроллера является верхним уровнем, и инициирует запуск процесса обработки другими типами контроллеров.
    /// Методы данного класса являются потокобезобасными, и используют механизм избирательной блокировки <see cref="ReaderWriterLockSlim"/>.
    /// Инициация событий на обработку обеспечивается асинхронно методами внутреннего класса <see cref="System.Threading.Timer"/>
    /// </summary>
    public abstract class AbstractInputProvider : AbstractController, IInputProvider
    {
        private Timer mTimer;
        private AutoResetEvent mStopWaitHandle;
        protected object mSyncRoot = new object();
        private ProcessStatusEnum mCurrentStatus;
        delegate void StopAsyncMethodCaller();
        [XmlElement]
        public int UpdateInterval { get; set; }
        [XmlElement]
        public Guid AdapterId { get; set; }
        /// <summary>
        /// Строка, определеющая путь источника получения данных
        /// </summary>
        [XmlElement]
        public string Source { get; set; }
        /// <summary>
        /// Коллекция контроллеров, реализующих логику интерфейса <see cref="IOutputProvider"/>. Определяет механизм вывода результата обработки. Предназначена для глобального применения ко всем заданияем, созданным данным объектом IInputProvider.
        /// Кроме экземпляров данной коллекции, подобные контроллеры могут быть добавлены и на локальном уровне продукта. Все они будут отработаны
        /// </summary>
        [XmlCollection(true, false, typeof(AbstractOutputProvider))]
        public ControllerCollection DefaultOutput { get; }
        /// <summary>
        /// Текущий статус выполнения процессов контроллером. Является потокобезопасным.
        /// </summary>
        public override ProcessStatusEnum CurrentStatus
        {
            get
            {
                if (mCurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                lock (mSyncRoot)
                {
                    return mCurrentStatus;
                }
            }
            protected set
            {
                if (mCurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                lock (mSyncRoot)
                {
                    mCurrentStatus = value;
                }
            }
        }
        /// <summary>
        /// После завершения работы метода Stop, внутренний объект Timer провайдера начнет процедуру остановки с последующим освобождением внутренних ресурсов объекта
        /// Это произойдет после завершения работы обработчика TimerCallback. Метод Stop возвращает соответствующий IAsyncResult состояния завершения всех процессов
        /// Операции потокобезопасны
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public virtual IAsyncResult Stop()
        {
            Log?.WriteInfo("AbstractInputProvider.Stop", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Запрос на завершения работы запускающего контроллера... Статус {mCurrentStatus}");
            if (mCurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            if (!Enabled)
            {
                Log?.WriteInfo("AbstractInputProvider.Stop", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Запускающий контроллер не требует остановки так как не был активирован");
                return null;
            }
            lock (mSyncRoot)
            {
                if (mTimer != null && mCurrentStatus == ProcessStatusEnum.Processing)
                {
                    mCurrentStatus = ProcessStatusEnum.Stopping;
                    void StopHandle()
                    {
                        if (!mTimer.Dispose(mStopWaitHandle))
                        {
                            mStopWaitHandle.Set();
                        }
                        else
                        {
                            Log?.WriteInfo("AbstractInputProvider.Stop", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Ожидание завершения процессов запускающего контроллера...");
                        }
                        WaitHandle.WaitAll(new WaitHandle[] { mStopWaitHandle });
                        CurrentStatus = ProcessStatusEnum.Idle;
                        Log?.WriteInfo("AbstractInputProvider.Stop", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Запускающий контроллер завершил работу");
                        mTimer = null;
                    }
                    StopAsyncMethodCaller caller = new StopAsyncMethodCaller(StopHandle);

                    return caller.BeginInvoke(null, null); 
                }

                return null;
            }
        }
        /// <summary>
        /// Метод вызова обработки события Timer
        /// </summary>
        /// <param name="_obj"></param>
        protected abstract void ProcessHandler(object _obj);
        /// <summary>
        /// Начинает выполнение определенного для данного контроллера, процесса
        ///  Если свойство Enabled имеет значение false, то при вызове метода никаиrе действия не будут выполнены, и метод вернет значение false
        /// </summary>
        public override void Start(object[] _settings)
        {
            if (mCurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);

            lock (mSyncRoot)
            {
                if (Enabled)
                {
                    Log?.WriteInfo("AbstractInputProvider.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Запускающий контроллер начал работу");

                    StartParameters = _settings;
                    mTimer = new Timer(ProcessHandler, _settings, 0, UpdateInterval);
                    mCurrentStatus = ProcessStatusEnum.Processing;
                }
            }
        }
        /// <summary>
        /// Освобождает внутренние ресурсы объекта. Переопределяет абстрактный метод базоваго класса
        /// </summary>
        /// <param name="_disposing"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        protected override void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (mCurrentStatus == ProcessStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                mStopWaitHandle.Close();
                Log?.WriteInfo("AbstractInputProvider.Dispose", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Запускающий контроллер => Ресурсы освобождены");
                if (DefaultOutput != null)
                {
                    foreach (var output in DefaultOutput)
                    {
                        output.Dispose();
                    }
                }
            }
            mCurrentStatus = ProcessStatusEnum.Disposed;
        }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_project"></param>
        protected AbstractInputProvider() : base()
        {
            DefaultOutput = new ControllerCollection(Owner?.Project, null);
            mCurrentStatus = ProcessStatusEnum.New;
            mStopWaitHandle = new AutoResetEvent(false);
            UpdateInterval = 5000;
        }
    }
}
