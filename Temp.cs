using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectPage
{
    internal class Temp
    {
        /*
        private IEnumerable<KeyValuePair<TextFragment, string>> FrameLinesAnalis()
        {
            List<KeyValuePair<TextFragment, string>> intervals = new List<KeyValuePair<TextFragment, string>>();
            //GdPicturePDF doc = Owner?.Owner?.Owner?.Owner?.Owner?.Owner?.Owner?.PdfObject;
            GdPicturePDF doc = null;

            // Исходные границы текстового фрейма. Опирамся на леый нижний угол. Размеры будем изменять по тексту
            // Первый с нулевыми размерами
            RectangleF area_bouns = new RectangleF(Bounds.X + Space, Bounds.Y + Space, 0, 0);
            float LineY = Bounds.Y + Space;
            float LineX = Bounds.X + Space;
            float LineH = 0;

            // Удобнее перебирать от конца коллекции, так как точка отсчета координат - левый нижний угол
            for (int j = TreeNodeItems.Count - 1; j >= 0; j--)
            {
                var item = TreeNodeItems[j];
                List<Glyph> input = ((TextLine)item).Glyphs;
                int lastindex = 0;

                // Перебор подряд всех глифов последовательно в каждой строке
                for (int i = 0; i < input.Count; i++)
                {
                    Glyph current = input[i];
                    RectangleF bounds = default;
                    // Последний символ строки. Завершаем анализ, и добавляем последний интервал
                    if (i == input.Count - 1)
                    {
                        string value = Encoding.UTF8.GetString(input.GetRange(lastindex, i - lastindex + 1).Select(x => x.Code).ToArray()).Replace(Environment.NewLine, "");

                        // Границы только текущего фрагмента текста
                        if (doc != null)
                        {
                            string fontName = doc.AddTrueTypeFont("TimesNewRoman", true, false, false);
                            // textWidth == 0 пустая строка
                            float textWidth = value != "" ? doc.GetTextWidth(fontName, current.Size, value) : 0;
                            // Выстота должна быть определена в любом случае
                            float textHeight = doc.GetTextHeight(fontName, current.Size);
                            WorkSpace.SystemLog.WriteError("FrameLinesAnalis 0", $" GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {current.Size}; fontName = {fontName}; value = {value}");
                            // Граници нового текста
                            bounds = new RectangleF(LineX, LineY, textWidth, textHeight);
                            // Суммирует к общей области
                            if (area_bouns.Height == 0)
                            {
                                area_bouns = bounds;
                            }
                            else
                            {
                                area_bouns = RectangleF.Union(area_bouns, bounds);
                            }
                            // Высота строки будет определяться высотой максимального фрагмента
                            LineH = Math.Max(LineH, bounds.Height);
                            // В начале строки (первый фрагмент новой строки) левая координата будет всегда постоянна - зависит только от расположения текстового фрагмента
                            LineX = Bounds.X + Space;
                        }
                        // Добавляем фрагмент
                        TextFragment key = new TextFragment() { Bounds = bounds, Position = lastindex, Line = item.Index, Paragraph = Index, Size = current.Size, Font = current.Font, FillColor = current.FillColor.FromUint32(), StrokeColor = current.StrokeColor.FromUint32(), StrokeWeight = current.StrokeWeight };
                        intervals.Add(new KeyValuePair<TextFragment, string>(key, value));
                        break;
                    }

                    Glyph next = input[i + 1];
                    // Впереди есть еще не менее одного символа, сравниваем параметры следующего с текущим
                    if (next.Size != current.Size || next.Font != current.Font || next.FillColor != current.FillColor || next.StrokeColor != current.StrokeColor || next.StrokeWeight != current.StrokeWeight)
                    {
                        // Параметры не совпали, выделяем предыдущий интервал в отдельный фрагмент
                        string value = Encoding.UTF8.GetString(input.GetRange(lastindex, i - lastindex + 1).Select(x => x.Code).ToArray()).Replace(Environment.NewLine, "");

                        // Границы только текущего фрагмента текста
                        if (doc != null)
                        {
                            string fontName = doc.AddTrueTypeFont("TimesNewRoman", true, false, false);
                            // textWidth == 0 пустая строка
                            float textWidth = value != "" ? doc.GetTextWidth(fontName, current.Size, value) : 0;
                            // Выстота должна быть определена в любом случае
                            float textHeight = doc.GetTextHeight(fontName, current.Size);
                            WorkSpace.SystemLog.WriteError("FrameLinesAnalis 1", $" GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {current.Size}; fontName = {fontName}; value = {value}");
                            // Граници нового текста
                            // 1. В начале строки (первый фрагмент новой строки) левая координата будет всегда постоянна - зависит только от расположения текстового фрагмента
                            // 2. Для последующих фрагментов начало это конец предыдущего (возможно с каким-то сдвигом, каким пока непонятно)
                            bounds = new RectangleF(LineX, LineY, textWidth, textHeight);
                            // Суммирует к общей области
                            if (area_bouns.Height == 0)
                            {
                                area_bouns = bounds;
                            }
                            else
                            {
                                area_bouns = RectangleF.Union(area_bouns, bounds);
                            }
                            // Высота строки будет определяться высотой максимального фрагмента
                            LineH = Math.Max(LineH, bounds.Height);
                            // Следующий фрагмент еще не новая строка
                            // Для последующих фрагментов начало это конец предыдущего (возможно с каким-то сдвигом, каким пока непонятно)
                            LineX = bounds.Right + mCharsSpace;
                        }
                        // Добавляем фрагмент
                        TextFragment key = new TextFragment() { Bounds = bounds, Position = lastindex, Line = item.Index, Paragraph = Index, Size = current.Size, Font = current.Font, FillColor = current.FillColor.FromUint32(), StrokeColor = current.StrokeColor.FromUint32(), StrokeWeight = current.StrokeWeight };
                        intervals.Add(new KeyValuePair<TextFragment, string>(key, value));
                        lastindex = i + 1;
                    }
                }
                if (LineH > 0)
                {
                    // Корректировка вертикальной координаты строки
                    LineY = LineY + LineH + Interval;
                }
            }

            if (doc != null)
            {
                Bounds = new RectangleF(area_bouns.X - Space, area_bouns.Y - Space, area_bouns.Width + 2 * Space, area_bouns.Height + 2 * Space);
            }

            return intervals;

        }
         
         */
        string[] ProcessTags = new string[166]
            {
                // File
                "FileName",
                "FileSize",
                "FileType",
                "MIMEType",
                "EncodingProcess",
                "FileModifyDate",
                "FileAccessDate",
                "FileCreateDate",
                "ExifByteOrder",
                "ColorComponents",
                "YCbCrSubSampling",
                "CurrentIPTCDigest",
                // File, EXIF, XMP, PNG
                "ImageWidth",
                "ImageHeight",
                // File, EXIF, XMP
                "BitsPerSample",
                // EXIF, Photoshop, JFIF, XMP
                "XResolution",
                "YResolution",
                // EXIF, JFIF, XMP
                "ResolutionUnit",
                // EXIF, XMP
                "ExifImageWidth",
                "ExifImageHeight",
                "Orientation",
                "PhotometricInterpretation",
                "PrimaryChromaticities",
                "ExposureTime",
                "CreateDate",
                "ColorSpace",
                "FocalLength",
                // EXIF, XMP, PNG
                "Compression", 
                // EXIF 
                "YCbCrPositioning",
                "YCbCrCoefficients",
                "ExposureTime",
                "ExposureMode",
                "ExposureProgram",
                "ExifVersion",
                "CompressedBitsPerPixel",
                "ShutterSpeedValue",
                "ApertureValue",
                "MaxApertureValue",
                "InteropIndex",
                "InteropVersion",
                "LensInfo",
                "LensMake",
                "LensModel",
                "GPSTimeStamp",
                "GPSDateStamp",
                "GPSSpeed",
                "GPSHPositioningError",
                // EXIF, Composite
                "GPSLatitude",
                "GPSLongitude",
                "GPSAltitude",
                // XMP
                "ICCProfileName",
                "ColorMode",
                "Format",
                // EXIF, XMP, MakerNotes, Composite
                "ISO",
                // EXIF, XMP, MakerNotes
                "MeteringMode",
                "ExposureCompensation",
                "WhiteBalance",
                "FNumber",
                "ExposureTime",
                "ColorSpace",
                // EXIF, MakerNotes
                "Contrast",
                "Saturation",
                // ICC_Profile
                "ProfileCMMType",
                "ColorSpaceData",
                "ProfileFileSignature",
                "ProfileDescription",
                "ProfileID",
                "ProfileConnectionSpace",
                "ConnectionSpaceIlluminant",
                "MediaWhitePoint",
                "ChromaticAdaptation",
                "Luminance",
                "RedTRC",
                "GreenTRC",
                "BlueTRC",
                "RedMatrixColumn",
                "GreenMatrixColumn",
                "BlueMatrixColumn",
                // APP14
                "DCTEncodeVersion",
                "APP14Flags0",
                "APP14Flags1",
                "ColorTransform",
                // Photoshop
                "HasRealMergedData",
                "PixelAspectRatio",
                "PhotoshopQuality",
                "PhotoshopFormat",
                "ProgressiveScans",
                "IPTCDigest",
                "LayerCount",
                "LayerRectangles",
                "LayerBlendModes",
                "LayerOpacities",
                "LayerVisible",
                "LayerColors",
                // Composite
                "Aperture",
                "ScaleFactor35efl",
                "ShutterSpeed",
                "CircleOfConfusion",
                "FOV",
                "FocalLength35efl",
                "HyperfocalDistance",
                "LightValue",
                "BlueBalance",
                "RedBalance",
                "GreenBalance",
                "Lens",
                "LensID",
                "ConditionalFEC",
                "GPSDateTime",
                // PNG
                "BitDepth",
                "ColorType",
                "Filter",
                "Interlace",
                "PixelsPerUnitX",
                "PixelsPerUnitY",
                "PixelUnits",
                // QuickTime
                "MajorBrand",
                "CompatibleBrands",
                "HandlerType",
                "PrimaryItemReference",
                "GeneralProfileSpace",
                "GeneralProfileIDC",
                "GenProfileCompatibilityFlags",
                "ConstraintIndicatorFlags",
                "GeneralLevelIDC",
                "ChromaFormat",
                "BitDepthLuma",
                "BitDepthChroma",
                "NumTemporalLayers",
                "TemporalIDNested",
                "ImageSpatialExtent",
                "Rotation",
                "ImagePixelDepth",
                "MetaImageSize",
                "MediaDataSize",
                "MediaDataOffset",
                // MakerNotes
                "Quality",
                "FocusMode",
                "RecordMode",
                "EasyMode",
                "FocusRange",
                "MaxFocalLength",
                "MinFocalLength",
                "FocalUnits",
                "MaxAperture",
                "MinAperture",
                "FlashActivity",
                "FlashBits",
                "ColorTone",
                "FocalType",
                "FocalPlaneXSize",
                "FocalPlaneYSize",
                "AutoISO",
                "BaseISO",
                "MeasuredEV",
                "TargetAperture",
                "TargetExposureTime",
                "SlowShutter",
                "CameraTemperature",
                "CameraType",
                "NDFilter",
                "CameraOrientation",
                "LensType",
                "ToneCurve",
                "ColorTemperature",
                "MeasuredRGGB"
            };
        string[] ProcessGroups = new string[12]
            {
                "File",
                "JFIF",
                "EXIF",
                "XMP",
                "ICC_Profile",
                "APP0",
                "APP14",
                "Photoshop",
                "Composite",
                "PNG",
                "QuickTime",
                "MakerNotes"
            };

    }
}
