using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;

namespace Smartproj
{
    public class JobDocument
    {
        public Dictionary<string, FontClass> Fonts { get; }
        public JobDocument() 
        {
            Fonts = new Dictionary<string, FontClass>();
        }    
    }
    /// <summary>
    /// Структура, содержащая все необходимае ссылки на объекты и параметры для выполнения конкретного процесса. Время жизни - пока происходит процесс.
    /// Выполяется от имени выбранного экземпляра проекта <see cref="Project"/>, и инициализируется методами запускающего контроллера <see cref="AbstractInputProvider"/> проекта.
    /// </summary>
    public class Job : IDisposable
    {
        private bool mIsDisposed;
        private object mSyncRoot = new Object();
        /// <summary>
        /// Разрешение с которым изображение должно быть автоматически образмерено. 0 - Не выполнять ничего
        /// </summary>
        public float AutoResample { get; set; }
        /// <summary>
        /// Минимально допустимое эффективное разрешение
        /// </summary>
        public float MinimalResolution { get; set; }
        private ProcessStatusEnum mStatus;
        /// <summary>
        /// Текущий статус процесса. Доступ потокобезопасен
        /// </summary>
        public ProcessStatusEnum Status { get { lock (mSyncRoot) { return mStatus; } } set { lock (mSyncRoot) { mStatus = value; } } }
        public Logger Log => Owner?.Log; 
        public Project Owner { get; }
        /// <summary>
        /// Номер заказа. 
        /// Свойство обеспечивает корректную работу, учитывая что каждый процесс по своей сути является конкретным заказом, который необходимо физически произвести. Соответственно, имеет уникальные параметры.
        /// </summary>
        public string OrderNumber { get; set; }
        /// <summary>
        /// Номер позиции заказа. 
        /// Свойство обеспечивает корректную работу, учитывая что каждый процесс по своей сути является конкретным заказом, который необходимо физически произвести. Соответственно, имеет уникальные параметры.
        /// </summary>
        public string ItemId { get; set; }
        /// <summary>
        /// Количество копий производства данного продукта. 
        /// Свойство обеспечивает корректную работу, учитывая что каждый процесс по своей сути является конкретным заказом, который необходимо физически произвести. Соответственно, имеет уникальные параметры.
        /// </summary>
        public int ProductionQty { get; set; }
        /// <summary>
        /// Путь к файлам процесса в области временных данных
        /// </summary>
        public string JobPath { get; private set; }
        /// <summary>
        /// Коллекция исходных данных (файлов, метаданных, параметров и т.п.) для работы процесса, представленных контейнерами <see cref="ExifTaggedFile"/>.
        /// По ходу последовательного выполнения контроллеров, данные контейнера могут корректироваться для управления поведением следующих контроллеров
        /// </summary>
        public List<ExifTaggedFile> DataContainer { get; }
        /// <summary>
        /// Коллекция сегментов данных <see cref="Segment"/>, необходимых для выполнения процесса.
        /// Сегмент представляет собой логическую структуру данных для группировки и анализу входных данных по определенному признаку
        /// </summary>
        public Segment Clusters { get; }
        /// <summary>
        /// Ссылка не структуру типа <see cref="Smartproj.Product"/>. Данная структура содержит абсолютно всю информацию для определения вида конечного продукта. Экземпляр создается путем десериализации в момент инициализации нового процесса
        /// </summary>
        public Product Product { get; private set; }
        /// <summary>
        /// Уникальный идентификатор процесса
        /// </summary>
        public Guid UID { get; }
        /// <summary>
        /// Тип метаданных которые могут быть получены и остпользованы для работы непосредственно из данных клиента
        /// </summary>
        public SourceParametersTypeEnum MetadataType { get; private set; }
        /// <summary>
        /// Метаданные, переданные клиентом, и содержащие информацию для процесса
        /// </summary>
        public object Metadata { get; private set; }
        /// <summary>
        /// Флаг, определяющий доступные для обработки типы файлов
        /// </summary>
        public TagFileTypeEnum FileDataFilter { get; private set; }
        /// <summary>
        /// Формат продукта в конечном виде по обрезному формату блока.
        /// Не зависит от способа внутренней раскладки и типа брошюровки. Также может не совпадать с размерами файла макета.
        /// Например, для продуккции типа 'Layflat' ширина макета будет в два раза больше формата блока продукта в финальном виде
        /// </summary>
        public Size ProductSize { get; private set; }
        /// <summary>
        /// Контейнер данных, представляющий собой результат работы контроллера сборки макета.
        /// Является инструкцией для работы контроллера <see cref="AbstractOutputProvider"/>
        /// </summary>
        public Dictionary<string, ImposedDataContainer> OutData { get; }
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="_project"></param>
        public Job(Project _project)
        {
            Owner = _project;
            mStatus = ProcessStatusEnum.New;
            mIsDisposed = false;
            UID = Guid.NewGuid();
            Clusters = new ExifTaggedFileSegments();
            DataContainer = new List<ExifTaggedFile>();
            OutData = new Dictionary<string, ImposedDataContainer>();
            JobPath = Path.Combine(Owner.ProjectPath, "Jobs", UID.ToString());
            Directory.CreateDirectory(JobPath);
            AutoResample = 0;
            MinimalResolution = 200;
        }
        /// <summary>
        /// Инициализация процесса, установки необходимых параметров для работы. Создает внутреннюю структуру продукта
        /// </summary>
        /// <param name="_product"></param>
        /// <param name="_productSize"></param>
        /// <param name="_metadata"></param>
        /// <param name="_metadataType"></param>
        /// <param name="_fileDataFilter"></param>
        public virtual void Create(Product _product, Size _productSize, object _metadata, SourceParametersTypeEnum _metadataType, TagFileTypeEnum _fileDataFilter)
        {
            MetadataType = _metadataType;
            FileDataFilter = _fileDataFilter;
            Metadata = _metadata;
            ProductSize = _productSize;
            Product = _product;
            Product.Owner = this;
            Product.CreateLayoutSpace(ProductSize);
        }
        protected void Dispose(bool _disposing)
        {
            if (_disposing)
            {
                if (mIsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
            }
            mIsDisposed = true;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~Job()
        {
            Dispose(false);
        }
    }
}
