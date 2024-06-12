using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Smartproj
{
    public class TemplateCollection : Tree
    {
        private Dictionary<int, KeyValuePair<Size, List<Template>>> mFrameHashIndex;
        private Dictionary<Size, KeyValuePair<int, List<Template>>> mFrameHashSize;
        public override Logger Log => Owner?.Log;
        public int Count => Degree;
        public Layout Owner { get; set; }
        private void FrameAddedEventHandler(object _sender, FrameAddedEventArgs _e)
        {
            Template sender = (Template)_sender;
            if (!sender.Enabled)
            {
                Log?.WriteWarning("FrameAddedEventHandler", $"Шаблон деактивирован '{sender.Name}'. Side = {sender.Side}; Id : {sender.UID}");
                return;
            }

            RectangleF area = _e.Area;
            Size size = new Size((int)Math.Round(area.Width), (int)Math.Round(area.Height));
            if (size.Width == 0 && size.Height == 0) return;

            // Добавление на обрез
            float x_new = area.X;
            float y_new = area.Y;
            float w_new = area.Width;
            float h_new = area.Height;

            if (_e.Side == PageSide.Left || _e.Side == PageSide.Single)
            {
                if (Math.Abs(_e.Area.X) < 1) x_new = -sender.Bleed;
            }
            if (Math.Abs(area.Y) < 1) y_new = -sender.Bleed;
            if (Math.Abs(area.Top - sender.Trim.Height) < 1) h_new = sender.Trim.Height + sender.Bleed - y_new;
            if (Math.Abs(area.Right - sender.Trim.Width) < 1) w_new = sender.Trim.Width + sender.Bleed - x_new;

            RectangleF correcredBleed = new RectangleF(x_new, y_new, w_new, h_new);
            if (correcredBleed != area)
            {
                area = correcredBleed;
                sender.Frames[_e.Side][_e.Position.X, _e.Position.Y] = area;
                size = new Size((int)Math.Round(area.Width), (int)Math.Round(area.Height));
            }
            //
            // Добавление фреймов в графику
            if (sender.Graphics == null)
            {
                sender.Graphics = new GraphicsCollection(sender);
            }
            if (!sender.Graphics.Where(x => x.GraphicType == GraphicTypeEnum.ImageFrame).Any(y => ((ImageFrame)y).FrameSide == _e.Side && ((ImageFrame)y).FrameID == _e.Position))
            {
                ImageFrame gf = (ImageFrame)sender.Graphics.Add(new ImageFrame() { Bounds = area, FrameID = _e.Position, FrameSide = _e.Side });
                // Если определено, сразу прикрепляем текстовые фреймы

                if (_e.PinnedParagraphs != null && _e.PinnedParagraphs.Count() > 0)
                {
                    gf.PinnedText = new ImageFrame.PinnedTextCollection(gf);
                    foreach (var paragraph in _e.PinnedParagraphs)
                    {
                        gf.PinnedText.AddFrame(paragraph);
                    }
                }
                _e.NewImageFrame = gf;
            }
            //

            KeyValuePair<int, List<Template>> value1;
            KeyValuePair<Size, List<Template>> value2;

            if (!mFrameHashSize.TryGetValue(size, out value1))
            {
                int index = mFrameHashSize.Count;
                value1 = new KeyValuePair<int, List<Template>>(index, new List<Template>());
                value2 = new KeyValuePair<Size, List<Template>>(size, new List<Template>());
                //
                mFrameHashSize.Add(size, value1);
                mFrameHashIndex.Add(index, value2);
            }
            else
            {
                value2 = mFrameHashIndex[value1.Key];
            }
            if (!value1.Value.Contains(sender))
            {
                value1.Value.Add(sender);
                value2.Value.Add(sender);
            }
        }
        public TemplateCollection(Layout _owner)
        {
            Owner = _owner;
            mFrameHashIndex = new Dictionary<int, KeyValuePair<Size, List<Template>>>();
            mFrameHashSize = new Dictionary<Size, KeyValuePair<int, List<Template>>>();
        }
        public new Template this[int _index]
        {
            get
            {
                if (_index < Degree)
                {
                    return (Template)TreeNodeItems[_index];
                }
                else
                    return null;
            }
        }
        public Template this[Guid _id] => (Template)TreeNodeItems.SingleOrDefault(x => ((Template)x).UID == _id);
        public override void Clear()
        {
            foreach (Template item in TreeNodeItems)
            {
                item.FrameAdded -= FrameAddedEventHandler;
            }
            base.Clear();
        }
        protected override void Insert(int _index, Tree _child)
        {
            if (_child != null)
            {
                base.Insert(_index, _child);
                Template item = (Template)_child;

                if (item.Frames != null)
                {
                    for (int i = 0; i < item.Frames.SidesCount; i++)
                    {
                        var frame = item.Frames[i];
                        for (int k = 0; k < frame.GetLength(0); k++)
                        {
                            for (int m = 0; m < frame.GetLength(1); m++)
                            {
                                if (frame[k, m] != default)
                                {
                                    PageSide side = PageSide.Single;
                                    if (!item.Frames.IsSingle)
                                    {
                                        side = i == 0 ? PageSide.Left : PageSide.Right;
                                    }
                                    FrameAddedEventHandler(item, new FrameAddedEventArgs(frame[k, m], side, new Point(k, m), null));
                                }
                            }
                        }
                    }
                }

                item.FrameAdded += FrameAddedEventHandler;
            }
        }
        public IDictionary<Size, KeyValuePair<int, List<Template>>> GetFrameAreas()
        {
            return new ReadOnlyDictionary<Size, KeyValuePair<int, List<Template>>>(mFrameHashSize);
        }
        public IDictionary<int, KeyValuePair<Size, List<Template>>> GetFrameIndexes()
        {
            return new ReadOnlyDictionary<int, KeyValuePair<Size, List<Template>>>(mFrameHashIndex);
        }
    }
    public class Template : Tree
    {
        public TemplateCollection Owner => (TemplateCollection)Parent;
        public event EventHandler<FrameAddedEventArgs> FrameAdded;
        [XmlElement]
        public BindingEnum BindingFilter { get; set; }
        [XmlElement]
        public DetailTypeEnum DetailFilter { get; set; }
        [XmlElement]
        public Guid UID { get; set; }
        [XmlElement]
        public bool Enabled { get; set; }
        [XmlElement]
        public int LeftW { get; set; } = -1;
        [XmlElement]
        public int LeftH { get; set; } = -1;
        [XmlElement]
        public int RightW { get; set; } = -1;
        [XmlElement]
        public int RightH { get; set; } = -1;
        [XmlElement]
        public int SingleW { get; set; } = -1;
        [XmlElement]
        public int SingleH { get; set; } = -1;
        [XmlElement]
        public DetailLayoutTypeEnum LayoutType { get; protected set; }
        [XmlElement]
        public PageSide Side { get; protected set; }
        [XmlElement]
        public SizeF Trim { get; protected set; }
        [XmlElement]
        public string Name { get; set; }
        [XmlElement]
        public float Bleed { get; set; }
        [XmlElement]
        public string Location { get; set; }
        [XmlElement]
        public float SafeCutZone { get; set; }
        [XmlCollection(true, false, typeof(GraphicItem), typeof(Template))]
        public GraphicsCollection Graphics { get; set; }
        [XmlCollection(true, false, typeof(GraphicItem), typeof(Template))]
        public TextCollection Texts { get; set; }
        [XmlCollection(false, false, typeof(RectangleF), typeof(Template))]
        public FrameCollection Frames { get; set; }
        internal virtual ImageFrame onFrameAdded(RectangleF _area, PageSide _side, Point _position, IEnumerable<TextFrame> _textParagraph)
        {
            ImageFrame ret = null;
            if (FrameAdded != null)
            {
                FrameAddedEventArgs args = new FrameAddedEventArgs(_area, _side, _position, _textParagraph);
                FrameAdded(this, args);
                ret = args.NewImageFrame;
            }
            return ret;
        }
        public void Save()
        {
            string path = Owner?.Owner?.Owner?.Owner?.Owner?.Owner?.Home;
            string templateName = "";

            if (path != null)
            {
                try
                {
                    if (Location != null && Location != "")
                    {
                        string directory = Path.GetDirectoryName(Location);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        templateName = Location;
                    }
                    else
                    {
                        string templateDir = Path.Combine(path, "Templates", $"{Owner.Owner.ProductSize.Width}x{Owner.Owner.ProductSize.Height}", $"{LayoutType}_{Side}", $"{Frames.FramesCount.ToString("00")}F");
                        if (!Directory.Exists(templateDir))
                        {
                            Directory.CreateDirectory(templateDir);
                        }
                        templateName = Path.Combine(templateDir, UID.ToString() + ".xml");
                        Location = templateName;
                    }

                    this.SaveXml(templateName);
                    Log?.WriteInfo("Template:Save", $"Конфигурации шаблона успешно сохранена '{templateName}'");
                }
                catch (Exception ex)
                {
                    Log?.WriteError("Template:Save", $"Ошибка сохранения конфигурации шаблона '{templateName}' => {ex.Message}");
                }
            }
        }
        public Template(DetailLayoutTypeEnum _layoutType, PageSide _side = PageSide.DefaultPage, SizeF _size = default)
        {
            LayoutType = _layoutType;
            Side = _side;
            Trim = _size;
            Bleed = 3;
            SafeCutZone = 5;
            Enabled = true;
            UID = Guid.NewGuid();
        }
        public Template(DetailLayoutTypeEnum _layoutType, PageSide _side, float _width, float _height) : this(_layoutType, _side, new SizeF(_width, _height))
        {
        }
        public Template(DetailLayoutTypeEnum _layoutType, float _width, float _height) : this(_layoutType, PageSide.DefaultPage, new SizeF(_width, _height))
        {
        }
        protected Template()
        {
            Enabled = true;
            Bleed = 3f;
            SafeCutZone = 5;
        }
    }
    public static class TemplateEx
    {
        public static void CreateFramesPreview(this Template _templ, IList<RectangleF> _data, string _file)
        {
            if (_data != null)
            {
                GdPicturePDF doc = new GdPicturePDF();

                doc.NewPDF(PdfConformance.PDF1_7);
                doc.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
                doc.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
                doc.SetJpegQuality(100);
                doc.EnableCompression(true);
                doc.SetOrigin(PdfOrigin.PdfOriginBottomLeft);
                string fontName = doc.AddStandardFont(PdfStandardFont.PdfStandardFontTimesBold);

                for (int i = 0; i < _data.Count; i++)
                {
                    doc.NewPage(_templ.Trim.Width + _templ.Bleed * 2, _templ.Trim.Height + _templ.Bleed * 2);
                    doc.SelectPage(i + 1);

                    doc.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, _templ.Trim.Width + _templ.Bleed * 2, _templ.Trim.Height + _templ.Bleed * 2);
                    doc.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, _templ.Bleed, _templ.Bleed, _templ.Trim.Width + _templ.Bleed, _templ.Trim.Height + _templ.Bleed);

                    doc.SetFillColor(Color.Black);
                    doc.DrawRectangle(_data[i].X + _templ.Bleed, _data[i].Y + _templ.Bleed, _data[i].Width, _data[i].Height, true, true);
                    float textWidth = doc.GetTextWidth(fontName, 30, i.ToString());
                    doc.SetTextSize(30);
                    doc.SetFillColor(Color.White);
                    doc.DrawText(fontName, _data[i].X + _templ.Bleed + _data[i].Width / 2 - textWidth / 2, _data[i].Y + _templ.Bleed + _data[i].Height / 2, i.ToString());
                }

                doc.SaveToFile(_file);
                doc.CloseDocument();
                doc.Dispose();
            }
        }
    }
}
