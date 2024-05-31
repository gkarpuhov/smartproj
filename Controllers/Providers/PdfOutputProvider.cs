using Emgu.CV.Linemod;
using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smartproj
{
    /// <summary>
    /// Контроллер, определяющий методы для вывода результата обработки в PDF файл. Наследует класс <see cref="AbstractOutputProvider"/>
    /// </summary>
    public class PdfOutputProvider : AbstractOutputProvider
    {
        public PdfOutputProvider() : base()
        {
        }
        public override bool Start(object[] _settings)
        {
            if (Enabled)
            {
                StartParameters = _settings;
                Job job = (Job)StartParameters[0];
                WorkSpace ws = job.Owner.Owner.Owner;
                Log?.WriteInfo("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер начал генерацию PDF файла... '{job.UID}'");

                string outPath = Path.Combine(job.JobPath, "~Output");
                Directory.CreateDirectory(outPath);

                foreach (var detaildata in job.OutData)
                {
                    string fileName = $"{job.OrderNumber}-{job.ItemId}-q{job.ProductionQty}-t{job.Owner.ProjectId}_{job.Product.ProductKeyCode}_{job.ProductSize.Width}X{job.ProductSize.Height}-{detaildata.Key}.pdf";

                    GdPicturePDF pdfObject = new GdPicturePDF();
                    if (pdfObject.NewPDF(PdfConformance.PDF1_7) != GdPictureStatus.OK)
                    {
                        Log?.WriteError("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: Ошибка создания экземпляра PDF документа (Job '{job.UID}')");
                        CurrentStatus = ProcessStatusEnum.Error;
                        return false;
                    }
                    pdfObject.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
                    pdfObject.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
                    pdfObject.SetJpegQuality(100);
                    pdfObject.EnableCompression(true);
                    pdfObject.SetOrigin(PdfOrigin.PdfOriginBottomLeft);

                    try
                    {
                        bool imposeSuccess = false;
                        switch (detaildata.Key)
                        {
                            case "BLK":
                            case "INS":
                                imposeSuccess = BlockImpose(pdfObject, detaildata.Value, job);
                                break;
                            case "CVR":
                                imposeSuccess = CoverImpose(pdfObject, detaildata.Value, job);
                                break;
                        }

                        if (imposeSuccess)
                        {
                            pdfObject.SaveToFile(Path.Combine(outPath, fileName));
                            Log?.WriteInfo("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => PDF файл '{fileName}' успешно создан (Job '{job.UID}')");
                        }
                        else
                        {
                            Log?.WriteError("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: Ошибка при выводе макета (Job '{job.UID}')");
                            CurrentStatus = ProcessStatusEnum.Error;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log?.WriteError("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: Обработанное исключении в процессе генерации макета '{ex.Message}' (Job '{job.UID}')");
                        CurrentStatus = ProcessStatusEnum.Error;
                        return false;
                    }
                    finally
                    {
                        pdfObject.CloseDocument();
                        pdfObject.Dispose();
                    }
                }

                if (Destination != null && Destination != "")
                {
                    Directory.CreateDirectory(Path.Combine(Destination, job.UID.ToString()));
                    foreach (var file in Directory.EnumerateFiles(outPath))
                    {
                        File.Copy(file, Path.Combine(Destination, job.UID.ToString(), Path.GetFileName(file)));
                    }
                }

                return true;
            }

            return false;
        }
        private bool CoverImpose(GdPicturePDF _pdfObject, ImposedDataContainer _detaildata, Job _job)
        {
            return true;
        }
        /// <summary>
        /// Метод сборки макета деталей типа "Блок" или "Персонализация блока"
        /// </summary>
        /// <param name="_pdfObject"></param>
        /// <param name="_detaildata"></param>
        /// <param name="_job"></param>
        /// <returns></returns>
        private bool BlockImpose(GdPicturePDF _pdfObject, ImposedDataContainer _detaildata, Job _job)
        {
            float bleedsize = -100;
            SizeF pagesize;
            // В случае брошюровка бабочка или или на нитку, макет PDF записывается разворотами. В этом случае размер макета по ширине равен удвоенному размеру изделия в готовом виде
            // В ином случае макет пополосный
            if (_job.Product.Binding == BindingEnum.LayFlat || _job.Product.Binding == BindingEnum.ThreadStitching)
            {
                pagesize = new SizeF(_job.ProductSize.Width * 2, _job.ProductSize.Height);
            }
            else
            {
                pagesize = _job.ProductSize;
            }

            int pageId = 0;

            foreach (var pdfpage in _detaildata)
            {
                if (pdfpage.Templ.LayoutType == DetailLayoutTypeEnum.Spread && ((int)Math.Round(pdfpage.Templ.Trim.Width / 2) != _job.ProductSize.Width || (int)Math.Round(pdfpage.Templ.Trim.Height) != _job.ProductSize.Height))
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Недопустимый формат разворотного шаблона {pdfpage.Templ.Trim.Width}x{pdfpage.Templ.Trim.Height} для продукта {_job.ProductSize.Width}x{_job.ProductSize.Height} (Job '{_job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }
                if ((pdfpage.Templ.LayoutType == DetailLayoutTypeEnum.Page || pdfpage.Templ.LayoutType == DetailLayoutTypeEnum.Single) && ((int)Math.Round(pdfpage.Templ.Trim.Width) != _job.ProductSize.Width || (int)Math.Round(pdfpage.Templ.Trim.Height) != _job.ProductSize.Height))
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Недопустимый формат постраничного шаблона {pdfpage.Templ.Trim.Width}x{pdfpage.Templ.Trim.Height} для продукта {_job.ProductSize.Width}x{_job.ProductSize.Height} (Job '{_job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }

                if (bleedsize == -100)
                {
                    bleedsize = pdfpage.Templ.Bleed;
                }
                if (bleedsize != pdfpage.Templ.Bleed)
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Отступы навылет доступных шаблонов должны совпадать (Job '{_job.UID}')");
                    CurrentStatus = ProcessStatusEnum.Error;
                    return false;
                }

                for (int i = 0; i < pdfpage.Imposed.Count; i++)
                {
                    // Делим шаблоны на страницы pdf
                    if (i == 0 || (pdfpage.Templ.Side & PageSide.LeftAndRight) != PageSide.LeftAndRight)
                    {
                        // Для добавления новой страницы должны быть выполнены условия:
                        // 1. Фреймы находятся на левой или единственной стороне шаблона (i == 0)
                        // ИЛИ
                        // 2. Фреймы находятся на правой стороне шаблона, но, при условии, что продукт не является бабочкой или КШС (PageSide имеет значение LeftAndRight). Макет в данном случае должен быть пополосным, и делить разворот на две страницы
                        _pdfObject.NewPage(pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                        _pdfObject.SelectPage(++pageId);
                        _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                        _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, bleedsize, bleedsize, pagesize.Width + bleedsize, pagesize.Height + bleedsize);
                        // Если новая страница является правой (i == 1), то эта (правая) сторона должна быть вырезана, и вставлена на отдельную страницу
                        // Исключение состовляет в случае продуктов типа бабочкой или КШС, но этот выриант исключается условным оператором данного блока кода

                        // Смещение влево при рисовании для разделения разворотного шаблона на страницы
                        // Так как координаты фреймов в разворотном шаблоне абсолютные и единые на весь разворот, мы определяем величину сдвига для правой полосы равную половине ширины шаблона
                        float xShift = _job.ProductSize.Width * i;

                        // После создания новой страницы начинаем заполнять ее элементами
                        List<GraphicItem> graphics = new List<GraphicItem>();
                        if (pdfpage.Templ.Graphics != null)
                        {
                            graphics.AddRange(pdfpage.Templ.Graphics);
                        }
                        if (pdfpage.Templ.Texts != null)
                        {
                            graphics.AddRange(pdfpage.Templ.Texts);
                        }

                        foreach (var layer in graphics.GroupBy(x => x.Layer).OrderBy(y => y.Key))
                        {
                            if (layer.Count() == 0) continue;
                            GraphicTypeEnum currentLayerType = layer.First().GraphicType;
                            // Начинаем перебирать графические элементы шаблона отсортированные по слоям в порядке приоритета
                            // Нужно учитывать, что если шаблон разворотный, то графический элемент может попадать на корешок, и присутствовать сразу на двух сторонах
                            // В таком случае, если разворот физически делится на две страницы, данный элемент нужно добавить два раза (на обе страницы)
                            if (i == 1)
                            {
                                // Сначала пробуем нарисовать элементы правой стороны (если она есть в отдельной файле) которые переходят через корешок с левой
                                // Только для правой стороны
                                foreach (var item in layer)
                                {
                                    if (item.ExtraBounds)
                                    {
                                        var rect = item.Bounds;
                                        // Обработка по типу слоя
                                        if (currentLayerType == GraphicTypeEnum.Fill)
                                        {
                                            if (item.GraphicType != GraphicTypeEnum.Fill)
                                            {
                                                Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Объект типа {item.GraphicType} не может быть расположен на {layer.Key} уровне (Job '{_job.UID}')");
                                                CurrentStatus = ProcessStatusEnum.Error;
                                                return false;
                                            }
                                            _pdfObject.SetFillColor(item.FillColor);
                                            _pdfObject.DrawRectangle(rect.X - xShift + bleedsize, rect.Y + bleedsize, rect.Width, rect.Height, true, false);
                                        }
                                        if (currentLayerType == GraphicTypeEnum.ImageFrame)
                                        {
                                            if (item.GraphicType != GraphicTypeEnum.ImageFrame)
                                            {
                                                Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Объект типа {item.GraphicType} не может быть расположен на {layer.Key} уровне (Job '{_job.UID}')");
                                                CurrentStatus = ProcessStatusEnum.Error;
                                                return false;
                                            }

                                            ImageFrame frame = (ImageFrame)item;
                                            // Если выставлен флаг item.ExtraBounds, элемент может быть только на левой стороне - 0
                                            ImposedImageData framedata = pdfpage.Imposed[0][frame.FrameID.X, frame.FrameID.Y];

                                            string filename = Path.Combine(_job.JobPath, "~Files", _job.DataContainer[framedata.FileId].GUID + ".jpg");
                                            // Координаты фреймов имеют ноль в обрезном формате. Вылет идет в минус. Поэтому сдвигаем на величину вылета (отнимая от общего смещения влево)
                                            FileToFrame(_pdfObject, rect, xShift, bleedsize, filename, framedata.Shift, framedata.Scale);
                                        }
                                    }
                                }
                            }

                            foreach (var item in layer)
                            {
                                // Перебор всех элементов шаблона на текущем слое
                                // По логике процесса понятно, что тут надо отфильтровать только те элементы, котореы нужно записать на новую страницу PDF, которую создали ранее
                                // Соответственно, фильтрация нужна, когда имеем разворотный шаблон, который физически делится на две страницы в файле.
                                var rect = item.Bounds;

                                int sideindex = -1;
                                if (pdfpage.Imposed.Count == 1)
                                {
                                    sideindex = 0;
                                }
                                if (sideindex == -1 && (pdfpage.Templ.Side & PageSide.LeftAndRight) == PageSide.LeftAndRight)
                                {
                                    sideindex = item.FrameSide == PageSide.Left ? 0 : 1;
                                }
                                if (sideindex == -1 && pdfpage.Imposed.Count == 2)
                                {
                                    if ((i == 0 && item.FrameSide == PageSide.Left) || (i == 1 && item.FrameSide == PageSide.Right)) sideindex = i;
                                }

                                if (sideindex != -1)
                                {
                                    // 1. pdfpage.Imposed.Count == 1:                                               один односторонний шаблон соответствует одной страницы pdf - любой листовой продукт, и пополосный шаблон фотокниг
                                    // 2. (pdfpage.Templ.Side & PageSide.LeftAndRight) == PageSide.LeftAndRight:    весь шаблон целиком разворотом на одну pdf страницу, неважно что сторон две
                                    // 3. При наличии двух сторон шаблона:
                                    // а) i == 0 && item.FrameSide == PageSide.Left:                                пропускаем только левые страницы двухстороннего шаблона
                                    // б) i == 1 && item.FrameSide == PageSide.Right:                               пропускаем только правые страницы двухстороннего шаблона

                                    // Обработка по типу слоя
                                    if (currentLayerType == GraphicTypeEnum.Fill)
                                    {
                                        if (item.GraphicType != GraphicTypeEnum.Fill)
                                        {
                                            Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Объект типа {item.GraphicType} не может быть расположен на {layer.Key} уровне (Job '{_job.UID}')");
                                            CurrentStatus = ProcessStatusEnum.Error;
                                            return false;
                                        }
                                        _pdfObject.SetFillColor(item.FillColor);
                                        _pdfObject.DrawRectangle(rect.X - xShift + bleedsize, rect.Y + bleedsize, rect.Width, rect.Height, true, false);
                                    }
                                    if (currentLayerType == GraphicTypeEnum.ImageFrame)
                                    {
                                        if (item.GraphicType != GraphicTypeEnum.ImageFrame)
                                        {
                                            Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Объект типа {item.GraphicType} не может быть расположен на {layer.Key} уровне (Job '{_job.UID}')");
                                            CurrentStatus = ProcessStatusEnum.Error;
                                            return false;
                                        }

                                        ImageFrame frame = (ImageFrame)item;
                                        ImposedImageData framedata = pdfpage.Imposed[sideindex][frame.FrameID.X, frame.FrameID.Y];

                                        string filename = Path.Combine(_job.JobPath, "~Files", _job.DataContainer[framedata.FileId].GUID + ".jpg");
                                        // Координаты фреймов имеют ноль в обрезном формате. Вылет идет в минус. Поэтому сдвигаем на величину вылета (отнимая от общего смещения влево)
                                        FileToFrame(_pdfObject, rect, xShift, bleedsize, filename, framedata.Shift, framedata.Scale);
                                    }
                                    if (currentLayerType == GraphicTypeEnum.TextFrame)
                                    {
                                        if (item.GraphicType != GraphicTypeEnum.TextFrame)
                                        {
                                            Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Объект типа {item.GraphicType} не может быть расположен на {layer.Key} уровне (Job '{_job.UID}')");
                                            CurrentStatus = ProcessStatusEnum.Error;
                                            return false;
                                        }

                                        TextFrame texts = (TextFrame)item;
                                        string fontName = _pdfObject.AddTrueTypeFontU("Montserrat", true, false, true);
                                        _pdfObject.SetFillColor(Color.Red);
                                        int c = 0;
                                        _pdfObject.SetTextSize(37);
                                        foreach (var text in texts.Lines)
                                        {
                                            _pdfObject.DrawText(fontName, 50, 100 - c * 10, text);
                                            c++;
                                        }
                                        //float textWidth = doc.GetTextWidth(fontName, 35, value);
                                        //float textHeight = doc.GetTextHeight(fontName, 35);
                                    }
                                }

                                // Только для варианта когда разворот режется на две страницы. Подходит для классических, флексов и псевдофлексов:
                                // Исключаем бабочки и КШС (PageSide имеет значение LeftAndRight)
                                // Исключаем неразворотные шаблоны - они могут быть как в листовой продукции, так и в книгах (в данном случае их тоже надо исключить)
                                if (i == 0 && (pdfpage.Templ.Side & PageSide.LeftAndRight) != PageSide.LeftAndRight && pdfpage.Imposed.Count == 2)
                                {
                                    // Сохраняем информация для элементов, переходящих черех корешок разворота
                                    item.ExtraBounds = rect.X + rect.Width > _job.ProductSize.Width;
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }
        private float FileToFrame(GdPicturePDF _doc, RectangleF _frame, float _shift, float _bleed, string _file, Point _correction, float _scale)
        {
            using (GdPictureImaging oImage = new GdPictureImaging())
            {
                int id = oImage.CreateGdPictureImageFromFile(_file);
                float iWidth = oImage.GetWidth(id);
                float iHeight = oImage.GetHeight(id);
                string imagename = _doc.AddImageFromGdPictureImage(id, false, false);
                float effectiveRes = 0f;

                _doc.SaveGraphicsState();
                try
                {
                    GdPictureStatus res;

                    PointF[] points = new PointF[4] 
                    { 
                        new PointF(_frame.X - _shift + _bleed, _frame.Y + _bleed), 
                        new PointF(_frame.X - _shift + _bleed, _frame.Y + _bleed + _frame.Height), 
                        new PointF(_frame.X - _shift + _bleed + _frame.Width, _frame.Y + _bleed + _frame.Height), 
                        new PointF(_frame.X - _shift + _bleed + _frame.Width, _frame.Y + _bleed)
                    };
                    _doc.AddGraphicsToPath(new GraphicsPath(points, new byte[4] { (byte)(PathPointType.Start | PathPointType.Line), (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line }));
                    _doc.ClipPath();

                    var fitedRect = _frame.FitToFrameF(iWidth, iHeight, _shift, _bleed);
                    res = _doc.DrawImage(imagename, fitedRect.Item1.X + _correction.X, fitedRect.Item1.Y + _correction.Y, fitedRect.Item1.Width, fitedRect.Item1.Height);
                    effectiveRes = fitedRect.Item2;

                    if (res != GdPictureStatus.OK)
                    {
                        Log?.WriteError("FileToFrame", $"{Owner?.Project?.ProjectId}: Ошибка отрисовки изображения во фрейм. Status = {res}");
                    }
                }
                catch (Exception ex)
                {
                    Log?.WriteError("FileToFrame", $"{Owner?.Project?.ProjectId}: Исключение при отрисовке изображения во фрейм '{ex.Message}'");
                }
                finally
                {
                    _doc.RestoreGraphicsState();
                    oImage.ReleaseGdPictureImage(id);
                }

                return effectiveRes;
            }
        }
    }
}
