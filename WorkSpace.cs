using GdPicture14;
using lcmsNET;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Smartproj
{
    /// <summary>
    /// Контейнер верхнего уровня. Инициализирует полность самодостаточную среду для работы системы, и определяет общие параметры
    /// </summary>
    public class WorkSpace : IDisposable
    {
        private bool mIsDisposed;
        public static readonly string ApplicationPath;
        public static readonly string ResourcesPath;
        public static readonly string WorkingPath;
        public static readonly Logger SystemLog;
        public static readonly string Fonts;
        //
        public readonly string MLData;
        public string Config;
        public readonly string Profiles;

        public readonly string Input;
        public readonly string Output;
        /// <summary>
        /// Сериализованный глобальный список доступных шрифтов <see cref="FontClass"/>
        /// По смыслу данный список имеет статус "статического", но для сериализации необходимо поместить его в экземпляр класса
        /// </summary>
        [XmlCollection(true, false, typeof(FontClass), typeof(WorkSpace))]
        public FontCollection ApplicationFonts { get; set; }
        /// <summary>
        /// Список определенных проектов <see cref="Project"/>
        /// </summary>
        [XmlCollection(true, false, typeof(Project), typeof(WorkSpace))]
        public ProjectCollection Projects { get; set; }
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
        }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public WorkSpace()
        {
            MLData = Path.Combine(ResourcesPath, "ML");
            Config = Path.Combine(ResourcesPath, "Config");

            Profiles = Path.Combine(ResourcesPath, "Profiles");
            Input = Path.Combine(WorkingPath, "Input");
            Output = Path.Combine(WorkingPath, "Output");

            foreach (var dir in new string[] { MLData, Config, Profiles, Input, Output })
            {
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            }
            mIsDisposed = false;
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
                            input.Start(this);
                        }
                    }
                }
                
            }
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
