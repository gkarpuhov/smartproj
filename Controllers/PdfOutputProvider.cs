using GdPicture14;
using Smartproj.Utils;
using System;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    /// <summary>
    /// Контроллер, определяющий методы для вывода результата обработки в PDF файл. Наследует класс <see cref="AbstractController"/> и реализует интерфейс <see cref="IInputProvider"/>
    /// </summary>
    public class PdfOutputProvider : AbstractOutputProvider
    {
        public PdfOutputProvider(DetailTypeEnum _detail) : base()
        {
            DetailType = _detail;
        }
        protected PdfOutputProvider() : base()
        {
            DetailType = DetailTypeEnum.Undefined;
        }
        /// <summary>
        /// Тип детали продукта, для которой определен данный контроллер
        /// </summary>
        [XmlElement]
        public DetailTypeEnum DetailType { get; protected set; }
        /// <summary>
        /// Ссылка на объект <see cref="GdPicturePDF"/>, содержащий PDF файл для данной детали
        /// </summary>
        public GdPicturePDF PdfObject { get; private set; }
        /// <summary>
        /// Активация контроллера для вывода макета в PDF файл. 
        /// Создает новый объект <see cref="GdPicturePDF"/> с характеристиками, соответствующими текущей комбинации экземпляров <see cref="Product"/>, <see cref="Job"/>, <see cref="Detail"/>
        /// Также метод проверяет все доступные шаблоны на несовпадение параметров размера и вылетов
        /// </summary>
        /// <param name="_settings"></param>
        public override bool Start(params object[] _settings)
        {
            if (Enabled)
            {
                StartParameters = _settings;
                CurrentStatus = ProcessStatusEnum.Processing;

                if (!base.Start(_settings))
                {
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }

                Job job = Owner?.Owner?.Owner;

                Product product = Owner?.Owner;
                if (product.LayoutSpace.Count == 0 || !product.LayoutSpace.Any(x => x.TemplateCollection.Count > 0))
                {
                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Не опеределено ни одного доступного шаблона (Job '{job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }

                PdfObject = new GdPicturePDF();
                if (PdfObject.NewPDF() != GdPictureStatus.OK)
                {
                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Ошибка создания экземпляра PDF документа (Job '{job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }
                PdfObject.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);

                SizeF size = default;
                float bleed = -100;

                // Определение формата документа
                foreach (var item in product.LayoutSpace)
                {
                    for (int i = 0; i < item.TemplateCollection.Count; i++)
                    {
                        if ((item.TemplateCollection[i].DetailFilter & DetailType) == DetailType)
                        {
                            SizeF current = (product.Binding == BindingEnum.LayFlat || product.Binding == BindingEnum.ThreadStitching) ? item.TemplateCollection[i].Trim : new SizeF((float)Math.Round(item.TemplateCollection[i].Trim.Width / 2), (float)Math.Round(item.TemplateCollection[i].Trim.Height / 2));

                            if (size == default)
                            {
                                size = current;
                            }
                            else
                            {
                                if (size.Width != current.Width || size.Height != current.Height)
                                {
                                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Размеры доступных шаблонов должны совпадать (Job '{job.UID}')");
                                    CurrentStatus = ProcessStatusEnum.Error;
                                    return false;
                                }
                            }
                            if (bleed == -100)
                            {
                                bleed = item.TemplateCollection[i].Bleed;
                            }
                            else
                            {
                                if (bleed != item.TemplateCollection[i].Bleed)
                                {
                                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Отступы навылет доступных шаблонов должны совпадать (Job '{job.UID}')");
                                    CurrentStatus = ProcessStatusEnum.Error;
                                    return false;
                                }
                            }
                        }
                    }
                }
                if (size == default || bleed == -100)
                {
                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Не определены размеры макета (Job '{job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }

                if (PdfObject.NewPage(size.Width + 2 * bleed, size.Height + 2 * bleed) != GdPictureStatus.OK)
                {
                    Log?.WriteError("PdfOutputProvider.Activate", $"Ошибка при активации контроллера создания PDF файла. Ошибка при добавлении страницы в PDF документ (Job '{job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }
                PdfObject.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, size.Width + 2 * bleed, size.Height + 2 * bleed);
                PdfObject.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, bleed, bleed, size.Width + bleed, size.Height + bleed);
                Log?.WriteInfo("PdfOutputProvider.Activate", $"PDF документ успешно инициализирован. Продукт: '{product.ProductCode}'; Деталь: {DetailType}; CropBox: {size.Width + 2 * bleed}x{size.Height + 2 * bleed} мм; Bleed: {bleed} мм (Job '{job.UID}')");

                CurrentStatus = ProcessStatusEnum.Finished;
                return true;
            }

            return false;
        }
        protected override void Dispose(bool _disposing)
        {
            base.Dispose(_disposing);
            if (_disposing)
            {
                if (PdfObject != null)
                {
                    PdfObject.Dispose();
                }
            }
        }
    }
}
