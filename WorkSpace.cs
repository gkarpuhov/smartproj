using GdPicture14;
using lcmsNET;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;

namespace Smartproj
{
    public class MailClientData
    {
        [XmlElement]
        public string Host { get; set; }
        [XmlElement]
        public string Usermail{ get; set; }
        [XmlElement]
        public string Username { get; set; }
        [XmlElement]
        public string Login { get; set; }
        [XmlElement]
        public string Password { get; set; }
        [XmlElement]
        public int Timeout { get; set; }
        public MailClientData()
        {
            Host = "smtp.yandex.ru";
            Usermail = "robot@fineart-print.ru";
            Username = "Поддержка FineArtPrint";
            Login = "robot@fineart-print.ru";
            Password = "MRG-vv7-7QM-9jn";
            Timeout = 5000;
        }
    }
    /// <summary>
    /// Контейнер верхнего уровня. Инициализирует полность самодостаточную среду для работы системы, и определяет общие параметры
    /// </summary>
    public class WorkSpace : IDisposable
    {
        private bool mIsDisposed;
        public static string ApplicationPath;
        public static string ResourcesPath;
        public static string WorkingPath;
        public static readonly Logger SystemLog;
        public static readonly MailLogger MailLogger;
        public static readonly string Fonts;
        //
        public readonly string MLData;
        public readonly string Config;
        public readonly string Profiles;
        [XmlContainer]
        public MailClientData SystemMail { get; set; }
        [XmlCollection(true, false, typeof(Press))]
        public PressCollection PressDevices { get;}
        /// <summary>
        /// 
        /// </summary>
        [XmlCollection(true, false, typeof(IAdapter))]
        public List<IAdapter> Adapters { get; }
        /// <summary>
        /// Сериализованный глобальный список доступных шрифтов <see cref="FontClass"/>
        /// По смыслу данный список имеет статус "статического", но для сериализации необходимо поместить его в экземпляр класса
        /// </summary>
        [XmlCollection(true, false, typeof(FontClass))]
        public FontCollection ApplicationFonts { get; }
        /// <summary>
        /// Список определенных проектов <see cref="Project"/>
        /// </summary>
        [XmlCollection(true, false, typeof(Project))]
        public ProjectCollection Projects { get; }
        /// <summary>
        /// Статический обработчик внутренних ошибок во внешнем модуле процессов цветоделения изображений
        /// </summary>
        /// <param name="contextID"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorText"></param>
        static void CmsHandleError(IntPtr contextID, int errorCode, string errorText) => SystemLog?.WriteError("CmsHandleError", $"ErrorCode = {errorCode}; ErrorText = {errorText}");
        /// <summary>
        /// Статический конструктор. Инициализация статических параметров внешних модулей, системы глобального логирования, управление лицензиями, установка расположения основных директорий работы приложения
        /// </summary>
        static WorkSpace()
        {
            GdPictureDeveloperKey.SetGdPictureDeveloperKey("211883860501001421116010749430779");
            GdPictureDocumentUtilities.SetAdaptiveFileCachingMechanism(true);
            Cms.SetErrorHandler(CmsHandleError);

            ApplicationPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            ResourcesPath = Path.Combine(ApplicationPath, "Resources");
            WorkingPath = Path.Combine(ApplicationPath, "Working");
            // Пока не вижу смысла в нескольких источников шрифтов для разных проектов или продуктов. Будем использовать общий
            // 1. Никто не мешает ограничить использование шрифтов на более низком уровне
            // 2. Так как метод SetCacheFolder статический, то физически в любом случае все продукты будут иметь доступ ко всем шрифтам
            Fonts = Path.Combine(ResourcesPath, "Fonts");
            if (!Directory.Exists(Fonts)) Directory.CreateDirectory(Fonts);
            GdPictureDocumentUtilities.AddFontFolder(Fonts);
            // То же самое касается расположения временных файлов обработки PDF
            string gdcache = Path.Combine(ApplicationPath, "Temp");
            if (!Directory.Exists(gdcache)) Directory.CreateDirectory(gdcache);
            GdPictureDocumentUtilities.SetCacheFolder(gdcache);

            SystemLog = new Logger();
            MailLogger = new MailLogger();
        }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public WorkSpace()
        {
            MLData = Path.Combine(ResourcesPath, "ML");
            Config = Path.Combine(ResourcesPath, "Config");

            Profiles = Path.Combine(ResourcesPath, "Profiles");

            foreach (var dir in new string[] { MLData, Config, Profiles})
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            mIsDisposed = false;

            Adapters = new List<IAdapter>();
            ApplicationFonts = new FontCollection(this);
            Projects = new ProjectCollection(this);
            PressDevices = new PressCollection();
            SystemMail = new MailClientData();
        }
        /// <summary>
        /// Инициирует работу запускающих контроллеров для доступных проектов <see cref="Project"/>
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void Initilize()
        {
            if (mIsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
            SystemLog.WriteInfo("WorkSpace.Initilize", $"Инициализация системы...");
            if (Projects != null)
            {
                foreach (var proj in Projects)
                {
                    if (proj.Enabled && proj.InputProviders != null)
                    {
                        foreach (AbstractInputProvider input in proj.InputProviders)
                        {
                            input.Start(null);
                        }
                    }
                }
            }
            //this.SaveXml(Path.Combine(ApplicationPath, "config.xml"));
        }
        /// <summary>
        /// Инициирует остановку всех запускающих контроллеров, и ожидает завершения всех связанных синхронных и асинхронных процессов
        /// Контроллеры остаются инициированны, и допускаю повторный запуск
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        public void StopAll()
        {
            if (mIsDisposed) throw new ObjectDisposedException(this.GetType().FullName);

            if (Projects != null && Projects.Count() > 0)
            {
                List<WaitHandle> handles = new List<WaitHandle>();
                foreach (var proj in Projects)
                {
                    if (proj.Enabled && proj.InputProviders != null)
                    {
                        foreach (AbstractInputProvider input in proj.InputProviders)
                        {
                            var hd = input.Stop().AsyncWaitHandle;
                            if (hd != null) handles.Add(hd);
                        }
                    }
                }
                WaitHandle.WaitAll(handles.ToArray());

                foreach (var wh in handles) wh.Close();
            }
        }
        /// <summary>
        /// Переопределяемый деструктор. Рекурсивно выполняет освобождение всех связанных ресурсов на нижних уровнях через метод <see cref="Project.Dispose()"/>
        /// </summary>
        /// <param name="_disposing"></param>
        /// <exception cref="ObjectDisposedException"></exception>
        protected virtual void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (mIsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
                if (Projects != null)
                {
                    foreach (var proj in Projects)
                    {
                        proj.Dispose(); 
                    }
                }
            }
            mIsDisposed = true;
        }
        /// <summary>
        /// Деструктор по умолчанию
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~WorkSpace()
        {
            Dispose(false);
        }
    }
}
