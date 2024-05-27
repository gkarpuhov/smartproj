using Emgu.CV.Linemod;
using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Printing;
using System.IO;
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
        /// Метдод сборки макета деталей типа "Блок" или "Персонализация блока"
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
            // Вспомогательная коллекция данных. В нее помещается информация о фреймах, которые попадают на корешок разворота и переходят с левой полы на правую
            // Если макет пополосный, то изображение данного фрейма дважды вставляется в файл
            var extraframes = new List<ValueTuple<RectangleF, string, Point, float>>();

            using (GdPictureImaging oImage = new GdPictureImaging())
            {
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
                        float xShift = 0;
                        if (i == 0 || !(_job.Product.Binding == BindingEnum.LayFlat || _job.Product.Binding == BindingEnum.ThreadStitching))
                        {
                            // Для добавления новой страницы должны быть выполнены условия:
                            // 1. Фреймы находятся на левой или единственной стороне шаблона (i == 0)
                            // 2. Фреймы находятся на правой стороне шаблона, но, при условии, что продукт не является бабочкой или КШС. Макет в данном случае должен быть пополосным, и делить разворот на две страницы
                            _pdfObject.NewPage(pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                            _pdfObject.SelectPage(++pageId);
                            _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                            _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, bleedsize, bleedsize, pagesize.Width + bleedsize, pagesize.Height + bleedsize);
                            // Если новая страница является правой (i == 1), то эта (правая) сторона должна быть вырезана, и вставлена на отдельную страницу
                            // Исключение состовляет в случае продуктов типа бабочкой или КШС, но этот выриант исключается условным оператором данного блока кода
                            // Так как координаты фремов в разворотном шаблоне абсолютные, мы определяем величину сдвига равную половине ширины шаблона
                            if (i == 1) xShift = _job.ProductSize.Width + bleedsize;
                        }

                        var side = pdfpage.Imposed[i];
                        extraframes.Clear();

                        for (int k = 0; k < side.GetLength(0); k++)
                        {
                            for (int m = 0; m < side.GetLength(1); m++)
                            {
                                if (pdfpage.Imposed[i][k, m].FileId == -1) continue;

                                string filename = Path.Combine(_job.JobPath, "~Files", _job.DataContainer[pdfpage.Imposed[i][k, m].FileId].GUID + ".jpg");

                                var rect = pdfpage.Templ.Frames[i][k, m];

                                if (i == 0 || !(_job.Product.Binding == BindingEnum.LayFlat || _job.Product.Binding == BindingEnum.ThreadStitching))
                                {
                                    // Сохраняем информация для фреймов, переходящих черех корешок разворота
                                    if (rect.X + rect.Width > _job.ProductSize.Width + bleedsize)
                                    {
                                        extraframes.Add(new ValueTuple<RectangleF, string, Point, float>(rect, filename, pdfpage.Imposed[i][k, m].Shift, pdfpage.Imposed[i][k, m].Scale));
                                    }
                                }

                                FileToFrame(_pdfObject, oImage, rect, xShift, filename, pdfpage.Imposed[i][k, m].Shift, pdfpage.Imposed[i][k, m].Scale);
                            }
                        }

                        if (extraframes.Count > 0)
                        {
                            _pdfObject.NewPage(pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                            _pdfObject.SelectPage(++pageId);
                            _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, pagesize.Width + 2 * bleedsize, pagesize.Height + 2 * bleedsize);
                            _pdfObject.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, bleedsize, bleedsize, pagesize.Width + bleedsize, pagesize.Height + bleedsize);

                            foreach (var frame in extraframes)
                            {
                                FileToFrame(_pdfObject, oImage, frame.Item1, _job.ProductSize.Width + bleedsize, frame.Item2, frame.Item3, frame.Item4);
                            }
                        }
                    }

                }
            }

            return true;
        }
        private float FileToFrame(GdPicturePDF _doc, GdPictureImaging _obj, RectangleF _frame, float _shift, string _file, Point _correction, float _scale)
        {
            int id = _obj.CreateGdPictureImageFromFile(_file);
            float iWidth = _obj.GetWidth(id);
            float iHeight = _obj.GetHeight(id);
            string imagename = _doc.AddImageFromGdPictureImage(id, false, false);
            float effectiveRes = 0f;

            _doc.SaveGraphicsState();
            try
            {
                GdPictureStatus res;

                PointF[] points = new PointF[4] { new PointF(_frame.X - _shift, _frame.Y), new PointF(_frame.X - _shift, _frame.Y + _frame.Height), new PointF(_frame.X - _shift + _frame.Width, _frame.Y + _frame.Height), new PointF(_frame.X - _shift + _frame.Width, _frame.Y) };
                _doc.AddGraphicsToPath(new GraphicsPath(points, new byte[4] { (byte)(PathPointType.Start | PathPointType.Line), (byte)PathPointType.Line, (byte)PathPointType.Line, (byte)PathPointType.Line }));
                _doc.ClipPath();

                var fitedRect = _frame.FitToFrameF(iWidth, iHeight, _shift);
                res = _doc.DrawImage(imagename, fitedRect.Item1.X + _correction.X, fitedRect.Item1.Y + _correction.Y, fitedRect.Item1.Width, fitedRect.Item1.Height);
                effectiveRes = fitedRect.Item2;

                if (res != GdPictureStatus.OK)
                {
                    Log?.WriteError("FileToFrame", $"Ошибка отрисовки изображения во фрейм. Status = {res}");
                }
            }
            catch (Exception ex)
            {
                Log?.WriteError("FileToFrame", $"Исключение при отрисовке изображения во фрейм '{ex.Message}'");
            }
            finally
            {
                _doc.RestoreGraphicsState();
                _obj.ReleaseGdPictureImage(id);
            }

            return effectiveRes;
        }
    }
}
