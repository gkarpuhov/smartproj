using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;

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
        public override void Start(object[] _settings)
        {
            if (Enabled)
            {
                CurrentStatus = ProcessStatusEnum.Processing;
                Job job = (Job)_settings[0];

                try
                {
                    StartParameters = _settings;
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
                            job.Status= ProcessStatusEnum.Error;
                            return;
                        }
                        pdfObject.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);

                        if (job.Product.Optimization == FileSizeOptimization.Lossless)
                        {
                            pdfObject.SetCompressionForColorImage(PdfCompression.PdfCompressionFlate);
                        }
                        else
                        {
                            pdfObject.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
                            switch (job.Product.Optimization)
                            {
                                case FileSizeOptimization.MaxQuality:
                                    pdfObject.SetJpegQuality(100);
                                    break;
                                case FileSizeOptimization.Medium:
                                    pdfObject.SetJpegQuality(75);
                                    break;
                                case FileSizeOptimization.Preview:
                                    pdfObject.SetJpegQuality(50);
                                    break;
                            }
                        }

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
                                job.Status = ProcessStatusEnum.Error;
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log?.WriteError("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: Обработанное исключении в процессе создания макета '{ex.Message}' (Job '{job.UID}')");
                            job.Status = ProcessStatusEnum.Error;
                            return;
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
                }
                catch (Exception ex)
                {
                    Log?.WriteError("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: Обработанное исключении в процессе '{ex.Message}' (Job '{job.UID}')");
                    job.Status = ProcessStatusEnum.Error;
                }
                finally
                {
                    CurrentStatus = ProcessStatusEnum.Finished;
                }
            }
            else
            {
                Log?.WriteInfo("PdfOutputProvider.Start", $"{Owner?.Project?.ProjectId}: '{this.GetType().Name}' => Контроллер деактивирован. Процессы не выполнены");
            }
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
                    return false;
                }
                if ((pdfpage.Templ.LayoutType == DetailLayoutTypeEnum.Page || pdfpage.Templ.LayoutType == DetailLayoutTypeEnum.Single) && ((int)Math.Round(pdfpage.Templ.Trim.Width) != _job.ProductSize.Width || (int)Math.Round(pdfpage.Templ.Trim.Height) != _job.ProductSize.Height))
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Недопустимый формат постраничного шаблона {pdfpage.Templ.Trim.Width}x{pdfpage.Templ.Trim.Height} для продукта {_job.ProductSize.Width}x{_job.ProductSize.Height} (Job '{_job.UID}')");
                    return false;
                }

                if (bleedsize == -100)
                {
                    bleedsize = pdfpage.Templ.Bleed;
                }
                if (bleedsize != pdfpage.Templ.Bleed)
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Отступы навылет доступных шаблонов должны совпадать (Job '{_job.UID}')");
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
                        _pdfObject.ResetGraphicsState();
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
                                        if (item.GraphicType == GraphicTypeEnum.Fill)
                                        {
                                            if (item.HasFill || item.HasStroke)
                                            {
                                                DrawFillItem(_pdfObject, (FillItem)item, xShift, bleedsize);
                                            }
                                        }
                                        if (item.GraphicType == GraphicTypeEnum.ImageFrame)
                                        {
                                            ImageFrame frame = (ImageFrame)item;
                                            // Если выставлен флаг item.ExtraBounds, элемент может быть только на левой стороне - 0
                                            ImposedImageData framedata = null;
                                            try
                                            {
                                                // Пока считаю операцию небезопасной
                                                framedata = pdfpage.Imposed[0][frame.FrameID.X, frame.FrameID.Y];
                                            }
                                            catch (Exception ex)
                                            {
                                                Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: {ex.Message} (Job '{_job.UID}')");
                                                return false;
                                            }
                                            string filename = Path.Combine(_job.JobPath, "~Files", _job.DataContainer[framedata.FileId].GUID + (_job.Product.Optimization == FileSizeOptimization.Lossless ? ".tiff" : ".jpeg"));
                                            // Координаты фреймов имеют ноль в обрезном формате. Вылет идет в минус. Поэтому сдвигаем на величину вылета (отнимая от общего смещения влево)
                                            DrawImageFrame(_pdfObject, frame, xShift, bleedsize, filename, framedata);
                                        }
                                        if (item.GraphicType == GraphicTypeEnum.TextFrame)
                                        {
                                            TextFrame text = (TextFrame)item;
                                            ImposedImageData framedata = null;
                                            if (text.PinObject != null)
                                            {
                                                try
                                                {
                                                    // Пока считаю операцию небезопасной
                                                    framedata = pdfpage.Imposed[0][text.PinObject.FrameID.X, text.PinObject.FrameID.Y];
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: {ex.Message} (Job '{_job.UID}')");
                                                    return false;
                                                }
                                            }
                                            DrawTextFrame(_pdfObject, text, framedata, _job, xShift, bleedsize);
                                        }
                                    }

                                    if (_job.Status == ProcessStatusEnum.Error) return false;
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
                                    if (item.GraphicType == GraphicTypeEnum.Fill)
                                    {
                                        if (item.HasFill || item.HasStroke)
                                        {
                                            DrawFillItem(_pdfObject, (FillItem)item, xShift, bleedsize);
                                        }
                                    }
                                    if (item.GraphicType == GraphicTypeEnum.ImageFrame)
                                    {
                                        ImageFrame frame = (ImageFrame)item;
                                        ImposedImageData framedata = null;
                                        try
                                        {
                                            // Пока считаю операцию небезопасной
                                            framedata = pdfpage.Imposed[sideindex][frame.FrameID.X, frame.FrameID.Y];
                                        }
                                        catch (Exception ex)
                                        {
                                            Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: {ex.Message} (Job '{_job.UID}')");
                                            return false;
                                        }
                                        string filename = Path.Combine(_job.JobPath, "~Files", _job.DataContainer[framedata.FileId].GUID + (_job.Product.Optimization == FileSizeOptimization.Lossless ? ".tiff" : ".jpeg"));
                                        // Координаты фреймов имеют ноль в обрезном формате. Вылет идет в минус. Поэтому сдвигаем на величину вылета (отнимая от общего смещения влево)
                                        DrawImageFrame(_pdfObject, frame, xShift, bleedsize, filename, framedata);
                                    }
                                    if (item.GraphicType == GraphicTypeEnum.TextFrame)
                                    {
                                        TextFrame text = (TextFrame)item;
                                        if (!text.IsEmpty)
                                        {
                                            ImposedImageData framedata = null;
                                            if (text.PinObject != null)
                                            {
                                                try
                                                {
                                                    // Пока считаю операцию небезопасной
                                                    framedata = pdfpage.Imposed[sideindex][text.PinObject.FrameID.X, text.PinObject.FrameID.Y];
                                                }
                                                catch (Exception ex)
                                                {
                                                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: {ex.Message} (Job '{_job.UID}')");
                                                    return false;
                                                }
                                            }
                                            DrawTextFrame(_pdfObject, text, framedata, _job, xShift, bleedsize);
                                        }
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

                                if (_job.Status == ProcessStatusEnum.Error) return false;
                            }
                        }
                    }
                }
            }

            return true;
        }
        protected void DrawTextFrame(GdPicturePDF _doc, TextFrame _item, ImposedImageData _framedata, Job _job, float _shift, float _bleed)
        {
            var rect = _item.Bounds;
            
            //WorkSpace ws = _job.Owner.Owner.Owner;
            List<ValueTuple<string, string, float, Color, Color>> testfordraw = null;

            // Привязан ли текст чему то?
            if (_framedata != null && !_item.ReadOnly)
            {
                string[] strings = null;
                // Пробуем поискать текст подмены
                // Если он не найдется, значит что-то не так - не будем ничего рисовать. Если надо нарисовать без привязки к фрейму, выставляем флаг ReadOnly
                _job = _framedata.Owner.Owner;
                var filedata = _job.DataContainer[_framedata.FileId];

                string txtFileName = Path.Combine(filedata.FilePath, Path.ChangeExtension(filedata.FileName, ".txt"));
                if (File.Exists(txtFileName))
                {
                    strings = File.ReadAllLines(txtFileName);
                    if (strings.Length == 0 || !strings.Any(x => x.Replace(" ", "") != "")) return;

                    testfordraw = UserTextData(_doc, _job, _item, strings);

                    if (testfordraw != null)
                    {
                        float width = 0;
                        float height = 0;

                        for (int i = 0; i < testfordraw.Count; i++)
                        {
                            float textWidth = _doc.GetTextWidth(testfordraw[i].Item2, testfordraw[i].Item3, testfordraw[i].Item1);
                            float textHeight = _doc.GetTextHeight(testfordraw[i].Item2, testfordraw[i].Item3);

                            width = Math.Max(width, textWidth);
                            height = height + textHeight;
                        }

                        Log?.WriteError("PdfOutputProvider.BlockImpose", $"_item.PinObject.Bounds: {_item.PinObject.Bounds}");

                        height = height + _item.Interval * (testfordraw.Count - 1);
                        width = width + 2 * _item.Space;
                        height = height + 2 * _item.Space;

                        Log?.WriteError("PdfOutputProvider.BlockImpose", $"size: {width}x{height}");

                        PointF topPoint = default;
                        switch (_item.PositionToImage.LinkTo)
                        {
                            case PositionEnum.TopLeft:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y);
                                break;
                            case PositionEnum.TopRight:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y);
                                break;
                            case PositionEnum.TopCenter:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width / 2, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y);
                                break;
                            case PositionEnum.CenterLeft:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height / 2);
                                break;
                            case PositionEnum.CenterRight:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height / 2);
                                break;
                            case PositionEnum.Center:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width / 2, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height / 2);
                                break;
                            case PositionEnum.BottomLeft:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height);
                                break;
                            case PositionEnum.BottomRight:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height);
                                break;
                            case PositionEnum.BottomCenter:
                                topPoint = new PointF(_item.PinObject.Bounds.X - _item.PositionToImage.Shift.X - width / 2, _item.PinObject.Bounds.Y - _item.PositionToImage.Shift.Y + height);
                                break;
                        }
                        Log?.WriteError("PdfOutputProvider.BlockImpose", $"topPoint: {topPoint}");


                        _doc.SaveGraphicsState();
                        try
                        {
                            float y = topPoint.Y - _item.Space;

                            for (int i = 0; i < testfordraw.Count; i++)
                            {
                                _doc.SetFillColor(testfordraw[i].Item4);
                                _doc.SetLineColor(testfordraw[i].Item5);
                                _doc.SetTextSize(testfordraw[i].Item3);

                                float textWidth = _doc.GetTextWidth(testfordraw[i].Item2, testfordraw[i].Item3, testfordraw[i].Item1);
                                float textHeight = _doc.GetTextHeight(testfordraw[i].Item2, testfordraw[i].Item3);
                                Log?.WriteError("PdfOutputProvider.BlockImpose", $"Text: {textWidth}x{textHeight}; size = {testfordraw[i].Item3}; x = {topPoint.X + _item.Space}; y = {y - textHeight}");

                                _doc.DrawText(testfordraw[i].Item2, topPoint.X + _item.Space, y - textHeight, testfordraw[i].Item1);

                                y = y - textHeight - _item.Interval;
                            }
                        }
                        finally
                        {
                            _doc.RestoreGraphicsState();
                        }
                    }
                    else
                    {
                        _job.Status = ProcessStatusEnum.Error;
                        return;
                    }
                }
                else
                {
                    Log?.WriteError("PdfOutputProvider.BlockImpose", $"{Owner?.Project?.ProjectId}: Не найдены даннные для замены текста в шаблоне. Операция не выполнена (Job '{_job.UID}')");
                    _job.Status = ProcessStatusEnum.Error;
                    return;
                }
            }
            else
            {
                // Просто рисуем текст из шаблона
            }
        }
        protected void DrawFillItem(GdPicturePDF _doc, FillItem _item, float _shift, float _bleed)
        {
            var rect = _item.Bounds;
            _doc.SaveGraphicsState();
            try
            {
                if (_item.HasFill) _doc.SetFillColor(_item.FillColor);
                if (_item.HasStroke) _doc.SetLineColor(_item.StrokeColor);

                if (_item.TransformationMatrix.Any(x => x != 0))
                {
                    _doc.AddTransformationMatrix(_item.TransformationMatrix[0], _item.TransformationMatrix[1], _item.TransformationMatrix[2], _item.TransformationMatrix[3], _item.TransformationMatrix[4], _item.TransformationMatrix[5]);
                }
                switch (_item.FrameShape)
                {
                    case ImageFrameShapeEnum.Rectangle:
                        _doc.DrawRectangle(rect.X - _shift + _bleed, rect.Y + _bleed, rect.Width, rect.Height, _item.HasFill, _item.HasStroke);
                        break;
                    case ImageFrameShapeEnum.Rounded:
                        _doc.DrawRoundedRectangle(rect.X - _shift + _bleed, rect.Y + _bleed, rect.Width, rect.Height, _item.Radius, _item.HasFill, _item.HasStroke);
                        break;
                    case ImageFrameShapeEnum.Ellipse:
                        _doc.DrawEllipse((rect.Left + rect.Right) / 2 - _shift + _bleed, (rect.Bottom + rect.Top) / 2 + _bleed, rect.Width, rect.Height, _item.HasFill, _item.HasStroke);
                        break;
                }
            }
            finally
            {
                _doc.RestoreGraphicsState();
            }
        }
        protected void DrawImageFrame(GdPicturePDF _doc, ImageFrame _frame, float _shift, float _bleed, string _file, ImposedImageData _imagedata)
        {
            var rect = _frame.Bounds;
            PointF correction = _imagedata.Shift;
            float scale = _imagedata.Scale;

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
                        new PointF(rect.X - _shift + _bleed, rect.Y + _bleed), 
                        new PointF(rect.X - _shift + _bleed, rect.Y + _bleed + rect.Height), 
                        new PointF(rect.X - _shift + _bleed + rect.Width, rect.Y + _bleed + rect.Height), 
                        new PointF(rect.X - _shift + _bleed + rect.Width, rect.Y + _bleed)
                    };
                    _doc.AddGraphicsToPath(new GraphicsPath(points, new byte[4] { (byte)(PathPointType.Start | PathPointType.Line), (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line }));
                    _doc.ClipPath();

                    var fitedRect = rect.FitToFrameF(iWidth, iHeight, _shift, _bleed);
                    res = _doc.DrawImage(imagename, fitedRect.Item1.X + correction.X, fitedRect.Item1.Y + correction.Y, fitedRect.Item1.Width, fitedRect.Item1.Height);
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
            }
        }
        private List<ValueTuple<string, string, float, Color, Color>> UserTextData(GdPicturePDF _doc, Job _job, TextFrame _textframe, string[] _usertext)
        {
            // Удаляем пустые строки в конце
            string[] usertext = _usertext.Reverse().SkipWhile(x => x.Replace(" ", "") == "").Reverse().ToArray();
            List<ValueTuple<string, string, float, Color, Color>> textForOut = new List<(string, string, float, Color, Color)>();

            // Устанавливаем соответствие между текстовым блоком шаблона и пользовательским
            int j = 0;
            int i = 0;
            ValueTuple<string, string, float, Color, Color> stringProp = default;

            while (j < usertext.Length)
            {
                // Вместо пустой строки добавляем пробел, чтобы у символа были явные параметры строки
                if (usertext[j] == "") usertext[j] = " ";
                // Если пользватель в своих данных оставил пустую строку, то пустое место не добавляет новые строки, а использует текущую строку шаблона (просто делает её пустой)
                if (i < _textframe.Degree)
                {
                    TextLine line = (TextLine)_textframe[i];
                    // Шаблоном можно регулировать добавление пропуска между строками путем добавление строки, содержащую служебную информацию %#%
                    if (line.Glyphs.Count > 0)
                    {
                        // Полностью пустые это непонятно что, таких не должно быть. Но проверить надо, если есть, то просто игнорируем
                        string fontname = GetPdfFont(_doc, _job, line.Glyphs[0].Font);
                        if (fontname != "")
                        {
                            if (line.Value != "%#%")
                            {
                                stringProp = new ValueTuple<string, string, float, Color, Color>(usertext[j], fontname, line.Glyphs[0].Size, line.Glyphs[0].FillColor.FromUint32(), line.Glyphs[0].StrokeColor.FromUint32());
                                textForOut.Add(stringProp);
                                // Пользовательская строка добавилась, значит можно брать следующую
                                j++;
                            }
                            else
                            {
                                textForOut.Add(new ValueTuple<string, string, float, Color, Color>(" ", fontname, line.Glyphs[0].Size, line.Glyphs[0].FillColor.FromUint32(), line.Glyphs[0].StrokeColor.FromUint32()));
                                // Пропуск строки
                            }
                        }
                        else
                        {
                            return null;
                        }
                    }
                    i++;
                }
                else
                {
                    // Пользовательских строк больше чем в шаблоне. Значит берем параметры последней строки
                    if (stringProp != default)
                    {
                        textForOut.Add(new ValueTuple<string, string, float, Color, Color>(usertext[j], stringProp.Item2, stringProp.Item3, stringProp.Item4, stringProp.Item5));
                        // Пользовательская строка добавилась, значит можно брать следующую
                        j++;
                    }
                }
            }

            return textForOut;
        }
        public static string GetPdfFont(GdPicturePDF _doc, Job _job, int _id)
        {
            WorkSpace ws = _job?.Owner?.Owner?.Owner;

            if (_job == null || ws == null) throw new NullReferenceException();

            foreach (var pair in _job.Document.Fonts)
            {
                if (pair.Value.Id == _id) return pair.Key;
            }

            var font = ws.ApplicationFonts[_id];
            if (font != null)
            {
                string fontname = _doc.AddTrueTypeFontU(font.Family, (font.Style & FontStyle.Bold) == FontStyle.Bold, (font.Style & FontStyle.Italic) == FontStyle.Italic, true);
                GdPictureStatus status = _doc.GetStat();
                if (status != GdPictureStatus.OK)
                {
                    _job.Log?.WriteError("GetPdfFont", $"{_job.Owner.ProjectId}: Ошибка при включении шрифта в PDF документ '{status}'  (Job '{_job.UID}')");
                    return "";
                }
                _job.Document.Fonts.Add(fontname, font);
                return fontname;
            }

            _job.Log?.WriteError("GetPdfFont", $"{_job.Owner.ProjectId}: Ошибка при включении шрифта в PDF документ: шрифт id = {_id} не найден  (Job '{_job.UID}')");

            return "";
        }
    }
}
