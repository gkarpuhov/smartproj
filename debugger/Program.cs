﻿using GdPicture14;
using lcmsNET;
using Smartproj;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.IO;
using System.Windows.Forms;

namespace debugger
{
    [Flags]
    public enum ImageStatusEnum
    {
        New = 0,
        Error = 1,
        NotSupported = 2,
        ExifData = 4,
        OriginalIsReady = 8,
        FormatTransformed = 16,
        ColorTransformed = 32,
        SizeTransformed = 64,
        SizeVerified = 128,
        FacesDetected = 256,
        Imposed = 512,
        Placed = 1024
    }
    public class Interval
    {
        public Interval(int _x1, int _x2)
        {
            X1 = _x1;
            X2 = _x2;
        }
        public Interval(ValueTuple<int, int> _interval) : this(_interval.Item1, _interval.Item2) { }
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Length => X2 - X1;
        public bool IsPoint => X1 == X2;
        public bool Contains(Interval _interval) => X2 >= _interval.X2 && X1 <= _interval.X1;
        public bool Contains(int _point) => X2 >= _point && X1 <= _point;
        public static bool Intersection(IEnumerable<Interval> _inetrvals, out Interval _intersection)
        {
            int x1 = int.MinValue;
            int x2 = int.MaxValue;
            _intersection = null;

            if (_inetrvals == null || _inetrvals.Count() == 0) return false;

            foreach (Interval interval in _inetrvals)
            {
                if (interval.X1 > x1) x1 = interval.X1;
                if (interval.X2 < x2) x2 = interval.X2;
                if (x1 > x2)
                {
                    return false;
                }
            }

            _intersection = new Interval(x1, x2);
            return true;
        }
    }

    internal class Program
    {

        public struct Glyph
        {
            public byte Code;
            public float Size;
            public int Font;
            public int Word;
            public int Line;
            public int Paragraph;
            public uint FillColor;
            public uint StrokeColor;
            public float StrokeWeight;
        }

        public struct GlyphStruct
        {
            public Glyph[] Data;
        }
        static ImageStatusEnum mStatus = ImageStatusEnum.New;
        public static ImageStatusEnum AddStatus(ImageStatusEnum _add)
        {
            return (mStatus = mStatus | _add);
        }
        public static bool HasStatus(ImageStatusEnum _status)
        {
            return (mStatus & _status) == _status;
        }
        public static bool IsOdd(int _value)
        {
            return _value % 2 == 0;
        }
        public class TestGlyph
        {
            [XmlElement]
            public byte[] Data { get; set; }
        }
        public static object COMCreateObject(string sProgID)
        {
            // We get the type using just the ProgID
            Type oType = Type.GetTypeFromProgID(sProgID);
            if (oType != null)
            {
                return Activator.CreateInstance(oType);
            }
            return null;
        }

        private async static Task TG_Update(ITelegramBotClient arg1, Update arg2, CancellationToken arg3)
        {
            if (arg2.Message.Text != null)
            {
                Console.WriteLine(arg2.Message.Text);
                return;
            }
        }
        private static Task TG_Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }
        static void Main(string[] args)
        {
            GdPicture14.GdPicturePDF doc = new GdPicturePDF();
            GdPictureDocumentUtilities.AddFontFolder(@"C:\Temp\using");
      
            doc.NewPDF();
            doc.NewPage(200, 200);
            //doc.SelectPage(1);
            string value = "TimesNewRoman";
            doc.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
            //string fontName = doc.AddTrueTypeFontFromFileU("ariblk.ttf", "", false, false, false);
            //string fontfamily = "Montserrat";
            //string fontName = doc.AddTrueTypeFontU(fontfamily, true, false, true);
            //Console.WriteLine($" GetStat = {doc.GetStat()}");
            //float textWidth = doc.GetTextWidth(fontName, 35, value);
            //float textHeight = doc.GetTextHeight(fontName, 35);
            //Console.WriteLine($"0: GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {35}; fontName = {fontName}; value = {value}; fontfamily = {fontfamily}");
            //
            PrivateFontCollection fontCollection = new PrivateFontCollection();
            string[] fontfiles = Directory.GetFiles(@"C:\Temp\using", "*.ttf");
            for (int i = 0; i < fontfiles.Length; i++)
            {
                string fontFilePath = fontfiles[i];
                fontCollection.AddFontFile(fontFilePath);

                byte[] fontData = System.IO.File.ReadAllBytes(fontFilePath);
                IntPtr fontPtr = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, fontPtr, fontData.Length);
                fontCollection.AddMemoryFont(fontPtr, fontData.Length);
                Marshal.FreeCoTaskMem(fontPtr);
            }
            Console.WriteLine($"fontCollection.Families.Length = {fontCollection.Families.Length}");
            

            for (int i = 0; i < fontCollection.Families.Length; i++)
            {
                Console.WriteLine($"Name = {fontCollection.Families[i].Name}");
                    
                if (fontCollection.Families[i].IsStyleAvailable(FontStyle.Bold))
                {
                    Font testFont = new Font(fontCollection.Families[i], 35, FontStyle.Bold, GraphicsUnit.Point);
                    Size len = TextRenderer.MeasureText(value, testFont);
                    Console.WriteLine($"len {value} pixels = {len.Width}x{len.Height}; mm = {(len.Width / 72f) * 25.4f}x{(len.Height / 72f) * 25.4f}");

                    Console.WriteLine($"Bold added = {testFont.Name}; Style = {testFont.Style}; SizeInPoints = {testFont.SizeInPoints}; Bold = {testFont.Bold}; Italic = {testFont.Italic}; {testFont.Height}");

                    string fontName = doc.AddTrueTypeFontU(fontCollection.Families[i].Name, true, false, true);
                    Console.WriteLine($" GetStat = {doc.GetStat()}");
                    float textWidth = doc.GetTextWidth(fontName, 35, value);
                    float textHeight = doc.GetTextHeight(fontName, 35);

                    Console.WriteLine($"0: GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {35}; fontName = {fontName}; value = {value}; fontfamily = {fontCollection.Families[i].Name}");
                }
                if (fontCollection.Families[i].IsStyleAvailable(FontStyle.Regular))
                {
                    Font testFont = new Font(fontCollection.Families[i], 35, FontStyle.Regular, GraphicsUnit.Point);
                    Console.WriteLine($"Regular added = {testFont.Name}; Style = {testFont.Style}; SizeInPoints = {testFont.SizeInPoints}; Bold = {testFont.Bold}; Italic = {testFont.Italic}; {testFont.Height}");

                    string fontName = doc.AddTrueTypeFontU(fontCollection.Families[i].Name, false, false, true);
                    Console.WriteLine($" GetStat = {doc.GetStat()}");
                    float textWidth = doc.GetTextWidth(fontName, 35, value);
                    float textHeight = doc.GetTextHeight(fontName, 35);

                    Console.WriteLine($"0: GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {35}; fontName = {fontName}; value = {value}; fontfamily = {fontCollection.Families[i].Name}");

                }
                if (fontCollection.Families[i].IsStyleAvailable(FontStyle.Italic))
                {
                    Font testFont = new Font(fontCollection.Families[i], 35, FontStyle.Italic, GraphicsUnit.Point);
                    Console.WriteLine($"Italic added = {testFont.Name}; Style = {testFont.Style}; SizeInPoints = {testFont.SizeInPoints}; Bold = {testFont.Bold}; Italic = {testFont.Italic}; {testFont.Height}");

                    string fontName = doc.AddTrueTypeFontU(fontCollection.Families[i].Name, false, true, true);
                    Console.WriteLine($" GetStat = {doc.GetStat()}");
                    float textWidth = doc.GetTextWidth(fontName, 35, value);
                    float textHeight = doc.GetTextHeight(fontName, 35);

                    Console.WriteLine($"0: GetStat = {doc.GetStat()}; textWidth = {textWidth}; textHeight = {textHeight}; Size = {35}; fontName = {fontName}; value = {value}; fontfamily = {fontCollection.Families[i].Name}");

                }
                Console.WriteLine();
            }




            //


        




            Console.ReadLine();
            return;

            var client = new TelegramBotClient("7010027401:AAGSPgWQ8SVvuIAceJJQWEFjMo70A63MNQg");
            client.StartReceiving(TG_Update, TG_Error);

            //TestGlyph obj = new TestGlyph();
            //d.ToCharArray().Select(c => (byte)c);

            //obj.Data = d.ToCharArray().Select(c => (byte)c).ToArray();
            //obj.SaveXml(@"c:\Temp\obj.xml");
            TestGlyph obj = (TestGlyph)Serializer.LoadXml(@"c:\Temp\obj.xml");
            string s = System.Text.Encoding.UTF8.GetString(obj.Data);
            Console.WriteLine(s);   
            /*
            Type oType = Type.GetTypeFromProgID("InDesign.Application.2020");
            if (oType != null)
            {
                Activator.CreateInstance(oType);
                InDesign.Application app = ObjectCaster<InDesign.Application>.As(Activator.CreateInstance(oType));

                //Document doc = (Document)app.Documents.Add();

                //ApplicationClass cs;
                //((Document)cs.Documents[0]).
                Console.WriteLine(app.GetType().FullName);
            }
        */
            //InDesignServer.Application app = new InDesignServer.ApplicationClass();
            //Document doc = (Document)app.Open(@"c:\temp\7B.idml");
            /*
            for (int i = 0; i < doc.Pages.Count; i++)
            {
                Page page = (Page)doc.Pages[i];
                Console.WriteLine($"page {i}: {page.Bounds.ToString()}");
                Rectangles frames = page.Rectangles;
                foreach (InDesignServer.Rectangle frame in frames)
                {
                    Console.WriteLine($"{frame.VisibleBounds.ToString()}: {frame.FillColor.ToString()}");
                }
            }
            */
            //Console.WriteLine(doc.Pages.Count);


            Tree tree1 = (Tree)Serializer.LoadXml(@"c:\Temp\tree.xml");

            for (int i = 0; i < tree1.Degree; i++)
            {
                Console.WriteLine($" {tree1[i].KeyId}");
                for (int j = 0; j < tree1[i].Degree; j++)
                {
                    Console.WriteLine($" -- {tree1[i][j].KeyId}");

                }
            }

            Console.ReadLine();
            return;

            string[] data = new string[] { "sa", "rrr", "toat", "aaa"};
            Tree tree = new Tree();
            for (int i = 0; i < data.Length; i++)
            {
                tree.Add(new Tree() { KeyId = data[i] });
            }

            string[] data1= new string[] { "333", "000", "111", "555" };
            for (int i = 0; i < data1.Length; i++)
            {
                tree[1].Add(new Tree() { KeyId = data1[i] });
            }
            tree.SaveXml(@"c:\Temp\tree.xml");




            string p = $@"C:\Users\g.karpuhov.FINEART-PRINT\source\repos\smartproj\bin\x64\Release\Resources\Config";
            WorkSpace ws = new WorkSpace();
            ws.Config = p;
            ws.Projects = new ProjectCollection(ws);
            Project mpp = ws.Projects.Add(new Project("MPP"));
            Project vru = ws.Projects.Add(new Project("VRU"));

            ws.SaveXml($@"C:\Users\g.karpuhov.FINEART-PRINT\source\repos\smartproj\bin\x64\Release\Resources\config.xml");

            //mpp.Products = new ProductCollection(mpp);
            //ClassicBook product = (ClassicBook)mpp.Products.Add((ClassicBook)Serializer.LoadXml(Path.Combine(p, "MPP", "Products", "73", "4bcc121e-694f-40fc-b44f-f889c293e83e_1.xml")));

            //product.SaveXml(Path.Combine(p, mpp.ProjectId, "Products", product.ProductCode, $"{product.UID}.xml"));



      





            /*
            Template template1 = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultPage, 400f, 280f);
            template1.Frames = new SpreadFrameCollection(1, 1, 1, 1, template1);
            template1.Name = "2 Фото 1";
            bool added1 = template1.Frames.Add(0);
            template1.Frames.SetSide(PageSide.Right);
            template1.BindingFilter = BindingEnum.Glue | BindingEnum.FlexBind;
            template1.DetailFilter = DetailTypeEnum.Block;
            added1 = template1.Frames.Add(1);
            */
   



            try
            {
                Layout layt = (Layout)Serializer.LoadXml(@"c:\Temp\templ5.xml");
                /*
                foreach (var t in layt.TemplateCollection)
                {
                    Console.WriteLine($"Name = {t.Name}");
                    Console.WriteLine($"Side = {t.Side}");
                    Console.WriteLine($"Bleed = {t.Bleed}");
                    Console.WriteLine($"Trim = {t.Trim}");
                    Console.WriteLine($"UID = {t.UID}");
                    Console.WriteLine($"BindingFilter = {t.BindingFilter}");
                    Console.WriteLine($"DetailFilter = {t.DetailFilter}");

                    foreach (var item in t.Frames)
                    {
                        Console.WriteLine($"item = {item}");
                    }

                    Console.WriteLine();
                }
                */
                foreach (var t in layt.TemplateCollection.GetFrameAreas())
                {
                    Console.WriteLine($"item = {t.Key}; {t.Value.Key}");
                }
                foreach (var t in layt.TemplateCollection.GetFrameIndexes())
                {
                    Console.WriteLine($"item = {t.Key}; {t.Value.Key}");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }







            Console.ReadLine();
            return;

            Profile rgbProfile = Profile.Open(@"C:\Windows\System32\spool\drivers\color\sRGB Color Space Profile.icm", "r");
            Profile cmykProfile = Profile.Open(@"C:\Windows\System32\spool\drivers\color\ISOcoated_v2_eci.icc", "r");
            Profile grayProfile = Profile.Open(@"C:\Windows\System32\spool\drivers\color\Generic Gray Gamma 2.2 Profile.icc", "r");

            Transform transform_CMYK_To_RGB = Transform.Create(cmykProfile, Cms.TYPE_CMYK_8, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
            Transform transform_Gray_To_RGB = Transform.Create(grayProfile, Cms.TYPE_GRAY_8, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
            Transform transform_RGB_To_RGB = Transform.Create(rgbProfile, Cms.TYPE_RGB_8, rgbProfile, Cms.TYPE_RGB_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);

            Transform transform_ARGB_To_RGB = Transform.Create(rgbProfile, Cms.TYPE_RGBA_8, rgbProfile, Cms.TYPE_RGB_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
            Transform transform_GrayA_To_RGB = Transform.Create(grayProfile, Cms.TYPE_GRAYA_8, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);

            Transform transform_CMYK_To_RGB_16 = Transform.Create(cmykProfile, Cms.TYPE_CMYK_16, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
            Transform transform_Gray_To_RGB_16 = Transform.Create(grayProfile, Cms.TYPE_GRAY_16, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);
            Transform transform_RGB_To_RGB_16 = Transform.Create(rgbProfile, Cms.TYPE_BGR_16, rgbProfile, Cms.TYPE_BGR_8, Intent.Perceptual, CmsFlags.BlackPointCompensation);


            using (Bitmap _img = new Bitmap(@"C:\Temp\project\RGB_16.tif", false))
            {
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, _img.Width, _img.Height);
                System.Drawing.Imaging.BitmapData tempData = _img.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, _img.PixelFormat);
                int bytes = Math.Abs(tempData.Stride) * _img.Height;
                byte[] cmykValues = new byte[bytes];

                byte[] rgbValues;
                Marshal.Copy(tempData.Scan0, cmykValues, 0, bytes);
                
                _img.UnlockBits(tempData);
                //RGBArray outValue = new RGBArray(rgbValues, _img.PixelFormat, _img.Width, _img.Height);

                using (Bitmap rgb = new Bitmap(_img.Width, _img.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb))
                {
                    System.Drawing.Imaging.BitmapData rgbData = rgb.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    rgbValues = new byte[Math.Abs(rgbData.Stride) * _img.Height];

                    Console.WriteLine($"Stride = {tempData.Stride}; PixelFormat = {_img.PixelFormat}; rgbValues = {cmykValues.Length}");
                    transform_RGB_To_RGB_16.DoTransform(cmykValues, rgbValues, _img.Width, _img.Height, tempData.Stride, rgbData.Stride, tempData.Stride, rgbData.Stride);

                    Marshal.Copy(rgbValues, 0, rgbData.Scan0, rgbValues.Length);
                    rgb.UnlockBits(rgbData);

                    rgb.Save(@"C:\Temp\CMYK_16.tif-CONV.jpg");
                }
                //Console.WriteLine($"Depth = {mat.Depth}; Cols = {mat.Cols}; Rows = {mat.Rows}; NumberOfChannels = {mat.NumberOfChannels}; Dims = {mat.Dims}; ElementSize = {mat.ElementSize}; Height = {mat.Height}; Width = {mat.Width}; Total = {mat.Total}; Size = {mat.Size}; SizeOfDimension = {String.Join(" ", mat.SizeOfDimension)}");
            }
            Console.ReadLine();
            
        }


    }
}