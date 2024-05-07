using Smartproj.Utils;
using System;
using System.Threading;

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
        protected ReaderWriterLockSlim mSyncRoot;
        private ControllerStatusEnum mCurrentStatus;
        delegate void StopAsyncMethodCaller();
        /// <summary>
        /// Строка, определеющая путь источника получения данных
        /// </summary>
        public string Source { get; set; }
        /// <summary>
        /// Коллекция контроллеров, реализующих логику интерфейса <see cref="IOutputProvider"/>. Определянт механизм вывода результата обработки. Предназначена для глобального применения ко всем заданияем, созданным данным объектом IInputProvider.
        /// Кроме экземпляров данной коллекции, подобные контроллеры могут быть добавлены и на локальном уровне продукта. Все они будут отработаны
        /// </summary>
        public ControllerColelction DefaultOutput { get; }
        /// <summary>
        /// Текущий статус выполнения процессов контроллером. Является потокобезопасным.
        /// </summary>
        public override ControllerStatusEnum CurrentStatus
        {
            get
            {
                if (mCurrentStatus == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                mSyncRoot.EnterReadLock();
                try
                {
                    return mCurrentStatus;
                }
                finally { mSyncRoot.ExitReadLock(); }
            }
            protected set
            {
                if (mCurrentStatus == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                mSyncRoot.EnterWriteLock();
                try
                {
                    mCurrentStatus = value;
                }
                finally { mSyncRoot.ExitWriteLock(); }
            }
        }
        [XmlContainer(typeof(AbstractInputProvider))]
        public IAdapter Adapter { get; set; }
        /// <summary>
        /// После завершения работы метода Stop, внутренний объект Timer провайдера начнет процедуру остановки с последующим освобождением внутренних ресурсов объекта
        /// Это произойдет после завершения работы обработчика TimerCallback. Метод Stop возвращает соответствующий IAsyncResult состояния завершения всех процессов
        /// Операции потокобезопасны
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        public IAsyncResult Stop()
        {
            if (mCurrentStatus == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            mSyncRoot.EnterWriteLock();
            try
            {
                if (mTimer != null && mCurrentStatus == ControllerStatusEnum.Processing)
                {
                    mCurrentStatus = ControllerStatusEnum.Stopping;
                    void StopHandle()
                    {
                        if (!mTimer.Dispose(mStopWaitHandle))
                        {
                            mStopWaitHandle.Set();
                        }
                        WaitHandle.WaitAll(new WaitHandle[] { mStopWaitHandle });
                        CurrentStatus = ControllerStatusEnum.Finished;
                        Log.WriteInfo("AbstractInputProvider.Stop", $"{this.GetType().Name}: контроллер завершил работу");
                        mTimer = null;
                    }
                    StopAsyncMethodCaller caller = new StopAsyncMethodCaller(StopHandle);

                    return caller.BeginInvoke(null, null); 
                }

                return null;
            }
            finally { mSyncRoot.ExitWriteLock(); }
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
        public override bool Start(params object[] _settings)
        {
            if (mCurrentStatus == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
            mSyncRoot.EnterWriteLock();
            try
            {
                if (Enabled)
                {
                    mTimer = new Timer(ProcessHandler, _settings, 0, 5000);
                    mCurrentStatus = ControllerStatusEnum.Processing;
                    Log.WriteInfo("AbstractInputProvider.Start", $"{this.GetType().Name}: контроллер начал работу");
                    return true;
                }
            }
            finally { mSyncRoot.ExitWriteLock(); }

            return false;
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
                if (mCurrentStatus == ControllerStatusEnum.Disposed) throw new ObjectDisposedException(this.GetType().FullName);
                mSyncRoot.Dispose();
                mStopWaitHandle.Close();
                Log.WriteInfo("AbstractInputProvider.Dispose", $"{this.GetType().Name}: ресурсы освобождены");
            }
            mCurrentStatus = ControllerStatusEnum.Disposed;
        }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_project"></param>
        protected AbstractInputProvider(Project _project) : base()
        {
            DefaultOutput = new ControllerColelction(_project, null);
            mCurrentStatus = ControllerStatusEnum.New;
            mSyncRoot = new ReaderWriterLockSlim();
            mStopWaitHandle = new AutoResetEvent(false);
        }
    }
}
