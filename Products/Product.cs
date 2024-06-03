using GdPicture14;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Smartproj
{
    public static class ProductEx
    {
        /*
        public static void CreateTempletesPreview(this Product _product, string _file)
        {
            if (_product != null)
            {
                GdPicturePDF doc = new GdPicturePDF();

                doc.NewPDF(PdfConformance.PDF1_7);
                doc.SetMeasurementUnit(PdfMeasurementUnit.PdfMeasurementUnitMillimeter);
                doc.SetCompressionForColorImage(PdfCompression.PdfCompressionJPEG);
                doc.SetJpegQuality(100);
                doc.EnableCompression(true);
                doc.SetOrigin(PdfOrigin.PdfOriginBottomLeft);

                int i = 0;
                foreach (var template in _product.TemplateKeys)
                {
                    doc.NewPage(template.Trim.Width + template.Bleed * 2, template.Trim.Height + template.Bleed * 2);
                    doc.SelectPage(++i);
                    doc.SetPageBox(PdfPageBox.PdfPageBoxCropBox, 0, 0, template.Trim.Width + template.Bleed * 2, template.Trim.Height + template.Bleed * 2);
                    doc.SetPageBox(PdfPageBox.PdfPageBoxTrimBox, template.Bleed, template.Bleed, template.Trim.Width + template.Bleed, template.Trim.Height + template.Bleed);

                    for (int k = 0; k < template.Frames.SidesCount; k++)
                    {
                        foreach (var rect in template.Frames[k])
                        {
                            if (rect != default)
                            {
                                doc.SetFillColor(Color.Black);
                                doc.DrawRectangle(rect.X + template.Bleed, rect.Y + template.Bleed, rect.Width, rect.Height, true, true);
                            }
                        }
                    }
                }

                doc.SaveToFile(_file);
                doc.CloseDocument();
                doc.Dispose();
            }
        }
        */
    }
    /*
    public class ProductCollection : Tree
    {
        public Project Owner { get; }
        public override Logger Log => Owner?.Log;
        public new Product this[int _index] => (Product)TreeNodeItems[_index];
        public Product this[Guid _index] => (Product)TreeNodeItems.Find(x => ((Product)x).UID == _index);
        protected override void Insert(int _index, Tree _child)
        {
            if (_child != null)
            {
                base.Insert(_index, _child);

                Product item = (Product)_child;
                item.CreateLayoutSpace();
                item.RIP.CreateImageProcessor();
                item.RIP.ImageProcessor.Converter = new ColorImagesConverter() { OutPath = Owner.ProjectPath, ProfilesPath = Owner.Owner.Owner.Profiles, ConverterLog = Log };
                item.RIP.ImageProcessor.ObjectDetector = new ObjectDetect() { DetectLog = Log, CascadesPath = Path.Combine(Owner.Owner.Owner.MLData, "haarcascades") };
            }
        }
        public ProductCollection(Project _owner)
        {
            Owner = _owner;
        }
    }
    */
    public abstract class Product : Tree
    {
        public string ProductKeyCode => (ProductCode != null && ProductCode != "") ? ProductCode.Split('_')[0] : ProductCode;
        public override Logger Log => Owner?.Log;
        public Job Owner { get; set; }
        public IEnumerable<Detail> Parts => TreeNodeItems.Cast<Detail>();
        public virtual string ProductCode { get; set; }
        [XmlElement]
        public FileSizeOptimization Optimization { get; set; }
        [XmlElement]
        public bool TempletesAutoUpdate { get; set; }
        public LayoutCollection LayoutSpace { get; set; }
        [XmlElement]
        public BindingEnum Binding { get; protected set; }
        [XmlElement]
        public Guid UID { get; set; }
        [XmlElement]
        public string Name { get; set; }
        [XmlCollection(false, false, typeof(Guid))]
        public List<Guid> TemplateKeys { get; set; }
        [XmlCollection(true, false, typeof(AbstractController))]
        public ControllerCollection Controllers { get; }
        public void CreateLayoutSpace() => CreateLayoutSpace(default);
        public void CreateLayoutSpace(Size _size)
        {
            LayoutSpace = new LayoutCollection(this);

            string templatesPath = Path.Combine(Owner.Owner.Home, "Templates");
            string[] dirs = Directory.GetDirectories(templatesPath, "*", SearchOption.TopDirectoryOnly);

            foreach (var dir in dirs)
            {
                Match match;

                if ((match = Regex.Match(Path.GetFileName(dir), @"(\d+)[xXхХ](\d+)", RegexOptions.Compiled)).Success)
                {
                    string[] files = Directory.GetFiles(dir, "*.xml", SearchOption.AllDirectories);
                    if (files.Length > 0)
                    {
                        Layout lay = null;
                        Size size = new Size(int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));

                        if (_size != default && _size != size) continue;

                        LayoutSpace.Add(lay = new Layout() { ProductSize = size });

                        if (lay.TemplateCollection == null)
                        {
                            lay.TemplateCollection = new TemplateCollection(lay);
                        }

                        foreach (var file in files)
                        {
                            try
                            {
                                Template template = (Template)Serializer.LoadXml(file);

                                template.Location = file;
                                bool isValid = TreeNodeItems.Find(x => (((Detail)x).DetailType & template.DetailFilter) == ((Detail)x).DetailType) != null && (Binding & template.BindingFilter) == Binding;
                                if (isValid && !TemplateKeys.Contains(template.UID))
                                {
                                    TemplateKeys.Add(template.UID);
                                }
                                if (isValid)
                                {
                                    if (template.Enabled && TemplateKeys.Contains(template.UID))
                                    {
                                        lay.TemplateCollection.Add(template);
                                    }
                                    if (TempletesAutoUpdate)
                                    {
                                        template.Save();
                                    }
                                }
                                else
                                {
                                    //Log?.WriteWarning("CreateLayoutSpace", $"Шаблон '{template.Name} ({size.Width}x{size.Height}: {template.UID})' дективирован или не соответствует типу продукции '{Name}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log?.WriteError("CreateLayoutSpace", $"Ошибка при загрузке шаблона ({size.Width}x{size.Height})'{Path.GetFileName(file)}: {ex.Message}");
                                Log?.WriteError("CreateLayoutSpace", $"Ошибка при загрузке шаблона ({size.Width}x{size.Height})'{Path.GetFileName(file)}: {ex.StackTrace}");
                            }
                        }
                        if (TempletesAutoUpdate)
                        {
                            Log?.WriteInfo("CreateLayoutSpace", $"Обновление шаблонов продукта выполнено {size.Width}x{size.Height}");
                        }
                        Log?.WriteInfo("CreateLayoutSpace", $"Загружено шаблонов {size.Width}x{size.Height}. Всего: {TemplateKeys.Count}; Добавлено: {lay.TemplateCollection.Count}");
                    }
                }
            }
        }
        protected Product() : base()
        {
            TempletesAutoUpdate = false;
            TemplateKeys = new List<Guid>();
            UID = Guid.NewGuid();
            Controllers = new ControllerCollection(null, this);
            Optimization = FileSizeOptimization.MaxQuality;
        }
        public bool HasPart(string _id)
        {
            return TreeNodeItems.Any(x => x.KeyId == _id);
        }
        public void Save()
        {
            string path = Path.Combine(Owner?.Owner?.Home, "Products", ProductCode);

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                this.SaveXml(Path.Combine(path, UID.ToString() + ".xml"));
                Log?.WriteInfo("Product:Save", $"Конфигурации продукта успешно сохранена '{Path.Combine(path, UID.ToString() + ".xml")}'");
            }
            catch (Exception ex) 
            {
                Log?.WriteError("Product:Save", $"Ошибка сохранения конфигурации продукта '{Path.Combine(path, UID.ToString() + ".xml")}' => {ex.Message}");
            }
        }
        public static string GetDefaultName(string _code)
        {
            string defaultProductName = "";
            string productCodeTemplate = @"^(3|5|6|7|CRD|SLM|FLD|CAL|PNR)(0|1|2|3|4|B|KHS|B2)(FBL|FBS|FI|V)?(T)?(W)?";
            var codeMatch = Regex.Match(_code, productCodeTemplate, RegexOptions.Compiled | RegexOptions.IgnoreCase);

            if (codeMatch.Success)
            {
                if (codeMatch.Groups[1].Value == "PNR")
                {
                    defaultProductName = "Панорама";
                }
                else
                if (codeMatch.Groups[1].Value == "CAL")
                {
                    defaultProductName = "Календарь";
                }
                else
                if (codeMatch.Groups[1].Value == "SLM")
                {
                    defaultProductName = "Карточки кашированные";
                }
                else
                if (codeMatch.Groups[1].Value == "CRD")
                {
                    defaultProductName = "Фотографии";
                }
                else
                if (codeMatch.Groups[1].Value == "FLD")
                {
                    defaultProductName = "Папка '" + (codeMatch.Groups[2].Value == "4" ? "Трюмо'" : "Планшет'");
                }
                else
                if (codeMatch.Groups[1].Value == "3")
                {
                    defaultProductName = "Мягкая обложка | " + (codeMatch.Groups[2].Value == "B" ? "Layflat" : "Брошюра");
                }
                else
                {
                    defaultProductName = "Твердая " + ((codeMatch.Groups[4].Value == "T" || codeMatch.Groups[5].Value == "W") ? "" : "фото") + "обложка";

                    switch (codeMatch.Groups[3].Value)
                    {
                        case "FBL": defaultProductName = defaultProductName + " | " + "Flexbind"; break;
                        case "FBS": defaultProductName = defaultProductName + " | " + "Flexbind"; break;
                        case "FI": defaultProductName = defaultProductName + " | " + "Блок с биговкой"; break;
                    }
                    switch (codeMatch.Groups[2].Value)
                    {
                        case "B": defaultProductName = defaultProductName + " | " + "Layflat"; break;
                        case "KHS": defaultProductName = defaultProductName + " | " + "КШС"; break;
                    }
                    if (codeMatch.Groups[2].Value == "3" && codeMatch.Groups[3].Value == "V")
                    {
                        defaultProductName = defaultProductName + " | " + "Блок с биговкой";
                    }
                    switch (codeMatch.Groups[1].Value)
                    {
                        case "5": defaultProductName = defaultProductName + " | " + "Комбинированная"; break;
                        case "6": defaultProductName = defaultProductName + " | " + "Цветной корешок"; break;
                        case "7": defaultProductName = defaultProductName + " | " + "Цельнокройная"; break;
                    }
                    if (codeMatch.Groups[5].Value == "W")
                    {
                        defaultProductName = defaultProductName + " с фотоокном";
                    }
                }
            }
            return defaultProductName;
        }
    }
}
