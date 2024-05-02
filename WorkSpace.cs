using GdPicture14;
using lcmsNET;
using Smartproj.Utils;
using System;
using System.Diagnostics;
using System.IO;

namespace Smartproj
{
    /// <summary>
    /// Контейнер верхнего уровня. Инициализирует систему и определяет общие параметры
    /// </summary>
    public class WorkSpace
    {
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
        /// Сериализованный глобальный список доступных шрифтов для того чтобы текстовые объекты могли связать внутрениий Id непосредственно с физическим шрифтом
        /// Проверить работоспособность шрифта из списка на данном этапе мы не можем. Возможные ошибки будут выявлены только на этапе работы с объектом PDF файла
        /// По смыслу данный список имеет статус "статического", но для сериализации необходимо поместить его в экземпляр объекта
        /// </summary>
        [XmlCollection(true, false, typeof(FontClass), typeof(WorkSpace))]
        public FontCollection ApplicationFonts { get; set; }
        /// <summary>
        /// Список определенных проектов. Смысловая нагрузка - обобщение набора продуктов, принадлежащих одному брэнду или клиенту/заказчику
        /// Данный контейнер не содержит сериализованной информации непосредственно о конкретном продукте
        /// </summary>
        [XmlCollection(true, false, typeof(Project), typeof(WorkSpace))]
        public ProjectCollection Projects { get; set; }
        static void CmsHandleError(IntPtr contextID, int errorCode, string errorText) => SystemLog?.WriteError("CmsHandleError", $"ErrorCode = {errorCode}; ErrorText = {errorText}");
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
        }
    }
}
