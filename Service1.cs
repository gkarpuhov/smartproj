using Emgu.CV.Dnn;
using GdPicture14;
using lcmsNET;
using smartproj.Products;
using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;

namespace Smartproj
{
    public partial class Service1 : ServiceBase
    {
        private WorkSpace Work;
        private Timer mTimer;
        private string mInPath;
        private string mOutPath;
        private object mSyncroot;
        private bool mIsRunning;
        public bool CheckForRun()
        {
            lock (mSyncroot)
            {
                if (!mIsRunning)
                {
                    mIsRunning = true;
                    return true;
                }
                else
                    return false;
            }
        }
        public void Unlock()
        {
            lock (mSyncroot)
            {
                mIsRunning = false;
            }
        }
        static Service1()
        {
            
        }
        public Service1()
        {
            InitializeComponent();
            mSyncroot = new object();
            mIsRunning = false;
        }
        private void MainEventHandler(object _state)
        {
            if (CheckForRun())
            {
                var infiles = Directory.GetFiles(mInPath, "*.zip", SearchOption.TopDirectoryOnly);

                if (infiles.Length > 0 && FileProcess.CheckForProcessFile(infiles))
                {

                    /*
                    Project project = Work.Projects["MPP"];
                    ClassicBook mpp = (ClassicBook)project.Products[Guid.Parse("4bcc121e-694f-40fc-b44f-f889c293e83e")];
                    mpp.Log.WriteInfo("Init", $@"TemplateCollection = {mpp.LayoutSpace.Sum(x => x.TemplateCollection.Count)}");


                    for (int i = 0; i < infiles.Length; i++)
                    {
                        System.IO.File.Move(infiles[i], Path.Combine(tempPath, Path.GetFileName(infiles[i])));
                    }
                    */

                    //mpp.RIP.ImageProcessor.ImportImageData(tempPath, true);
                    //mpp.RIP.ImageProcessor.ConvertAll();
                    if (Work != null) return;

                    //Work = new WorkSpace();
                    Work = (WorkSpace)Serializer.LoadXml(Path.Combine(WorkSpace.ApplicationPath, "config.xml"));
                    //Work.Projects = new ProjectCollection(Work);
                    //Project vru = new Project("VRU");
                    Project vru = Work.Projects["VRU"];
                    //Work.Projects.Add(vru);
                    try
                    {
                        vru.Log.WriteError("", "0");
                        string tempPath = Path.Combine(vru.ProjectPath, "~Original");
                        Directory.CreateDirectory(tempPath);
                        for (int i = 0; i < infiles.Length; i++)
                        {
                            System.IO.File.Move(infiles[i], Path.Combine(tempPath, Path.GetFileName(infiles[i])));
                        }

                        Size size = new Size(200, 280);
                        vru.Log.WriteError("", "1");
                        Job job = new Job(vru);
                        LayFlat lf = new LayFlat("7B", "");
                        lf.Add(new CoverDetail() { LayoutType = DetailLayoutTypeEnum.Lainer });
                        lf.Add(new BlockDetail() { LayoutType = DetailLayoutTypeEnum.Spread });
                        job.Create(lf, size);
                        vru.Log.WriteError("", "2");
                        //LayFlat lf = (LayFlat)job.Product;
           
                        vru.Log.WriteError("", "25");
                        /*
                        // read mode
                        Layout layout = lf.LayoutSpace[size];
                        for (int i = 0; i < layout.TemplateCollection.Count; i++)
                        {
                            vru.Log.WriteError("", layout.TemplateCollection[i].Location);
                        }
                        Template ttt = layout.TemplateCollection[Guid.Parse("fd9d9ce1-fb1f-457a-9829-5bc4facb5ad8")];
                        foreach (var text in ((ImageFrame)ttt.Graphics[0]).PinnedText)
                        {
                            vru.Log.WriteError("", text);
                        }
                        vru.Log.WriteError("", $"Text frame size 1 = {((TextFrame)ttt.Texts[0]).Bounds}");

                        return;
                        */
                        var fonts = vru.Owner.Owner.ApplicationFonts = new FontCollection(Work);
                        fonts.Add(new FontClass("Arial", FontStyle.Bold, ""));

                        TextParameters textParameter = new TextParameters() { FillColor = Color.Black, StrokeColor = Color.Transparent, Size = 35, StrokeWeight = 0, Font = new FontClass("Arial", FontStyle.Bold, "") };
                        vru.Log.WriteError("", "3");
                        lf.LayoutSpace = new LayoutCollection(lf);
                        Layout layout;
                        lf.LayoutSpace.Add(layout = new Layout() { ProductSize = size });
                        layout.TemplateCollection = new TemplateCollection(layout);
                        vru.Log.WriteError("", "4");
                        Template t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, 400, 280) { DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert, BindingFilter = BindingEnum.LayFlat };

                        layout.TemplateCollection.Add(t);

                        t.Frames = new SpreadFrameCollection(1, 1, 2, 2, t);
                        vru.Log.WriteError("", "5");

                        TextFrame tf = new TextFrame() { FillColor = Color.White, Interval = 2, Position = VerticalPositionEnum.Center, ReadOnly = false, Space = 2, Bounds = new RectangleF(0, 0, 100, 50), StrokeColor = Color.Transparent };
                        tf.AddLines("ГРИГОРИЙ\r\nКАРПУХОВ", new TextParameters() { FillColor = Color.Black, StrokeColor = Color.Transparent, Size = 35, StrokeWeight = 0, Font = new FontClass("Arial", FontStyle.Bold, ""), Paddling = HorizontalPositionEnum.Left });

                        ImageFrame gf = t.Frames.AddLayoutSegment(new RectangleF(19, 51, 161, 232), x => new PointF(0, -(x.Bounds.Height + 20)), tf);

                        t.Frames.SetSide(PageSide.Right);

                        t.Frames.Add(new RectangleF(213, 172, 62, 86));
                        t.Frames.Add(new RectangleF(278, 172, 108, 86));
                        t.Frames.Add(new RectangleF(213, 23, 173, 146));

                        lf.TemplateKeys.Add(t.UID);

                        t.Save();
                        vru.Log.WriteError("", "6");
                        t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, 400, 280) { DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert, BindingFilter = BindingEnum.LayFlat };

                        layout.TemplateCollection.Add(t);

                        t.Frames = new SpreadFrameCollection(1, 1, 4, 3, t);
                        t.Frames.Add(new RectangleF(19, 51, 161, 232));
                        t.Frames.SetSide(PageSide.Right);

                        t.Frames.Add(new RectangleF(220, 198, 38, 47));
                        t.Frames.Add(new RectangleF(261, 198, 38, 47));
                        t.Frames.Add(new RectangleF(302, 198, 38, 47));
                        t.Frames.Add(new RectangleF(343, 198, 38, 47));

                        t.Frames.Add(new RectangleF(220, 119, 38, 47));
                        t.Frames.Add(new RectangleF(261, 119, 38, 47));
                        t.Frames.Add(new RectangleF(302, 119, 38, 47));
                        t.Frames.Add(new RectangleF(343, 119, 38, 47));

                        t.Frames.Add(new RectangleF(220, 40, 38, 47));
                        t.Frames.Add(new RectangleF(261, 40, 38, 47));
                        t.Frames.Add(new RectangleF(302, 40, 38, 47));
                        t.Frames.Add(new RectangleF(343, 40, 38, 47));

                        lf.TemplateKeys.Add(t.UID);
                        t.Save();

                        t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, 400, 280) { DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert, BindingFilter = BindingEnum.LayFlat };

                        layout.TemplateCollection.Add(t);

                        t.Frames = new SpreadFrameCollection(2, 2, 2, 2, t);
                        t.Frames.Add(new RectangleF(12, 162, 82, 106));
                        t.Frames.Add(new RectangleF(105, 162, 82, 106));
                        t.Frames.Add(new RectangleF(12, 31, 82, 106));
                        t.Frames.Add(new RectangleF(105, 31, 82, 106));

                        t.Frames.SetSide(PageSide.Right);

                        t.Frames.Add(new RectangleF(213, 162, 82, 106));
                        t.Frames.Add(new RectangleF(306, 162, 82, 106));
                        t.Frames.Add(new RectangleF(213, 31, 82, 106));
                        t.Frames.Add(new RectangleF(306, 31, 82, 106));

                        lf.TemplateKeys.Add(t.UID);
                        t.Save();

                        t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, 400, 280) { DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert, BindingFilter = BindingEnum.LayFlat };

                        layout.TemplateCollection.Add(t);

                        t.Frames = new SpreadFrameCollection(2, 2, 2, 3, t);
                        t.Frames.Add(new RectangleF(12, 162, 82, 106));
                        t.Frames.Add(new RectangleF(105, 162, 82, 106));
                        t.Frames.Add(new RectangleF(12, 31, 82, 106));
                        t.Frames.Add(new RectangleF(105, 31, 82, 106));

                        t.Frames.SetSide(PageSide.Right);

                        t.Frames.Add(new RectangleF(213, 142, 175, 126));
                        t.Frames.Add(default);
                        t.Frames.Add(new RectangleF(213, 78, 92, 61));
                        t.Frames.Add(new RectangleF(308, 78, 80, 61));
                        t.Frames.Add(new RectangleF(213, 14, 87, 61));
                        t.Frames.Add(new RectangleF(303, 14, 85, 61));

                        lf.TemplateKeys.Add(t.UID);
                        t.Save();

                        t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, 400, 280) { DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert, BindingFilter = BindingEnum.LayFlat };

                        layout.TemplateCollection.Add(t);

                        t.Frames = new SpreadFrameCollection(2, 3, 2, 3, t);
                        t.Frames.Add(new RectangleF(12, 207, 87, 61));
                        t.Frames.Add(new RectangleF(102, 207, 85, 61));
                        t.Frames.Add(new RectangleF(12, 78, 175, 126));
                        t.Frames.Add(default);
                        t.Frames.Add(new RectangleF(12, 14, 87, 61));
                        t.Frames.Add(new RectangleF(102, 14, 85, 61));

                        t.Frames.SetSide(PageSide.Right);

                        t.Frames.Add(new RectangleF(213, 207, 87, 61));
                        t.Frames.Add(new RectangleF(303, 207, 85, 61));
                        t.Frames.Add(new RectangleF(213, 78, 175, 126));
                        t.Frames.Add(default);
                        t.Frames.Add(new RectangleF(213, 14, 87, 61));
                        t.Frames.Add(new RectangleF(303, 14, 85, 61));

                        lf.TemplateKeys.Add(t.UID);
                        t.Save();
                        vru.Log.WriteError("", "7");
                        lf.Save();
                        vru.Log.WriteError("", "8");
                        Work.SaveXml(Path.Combine(WorkSpace.ApplicationPath, "config.xml"));
                    }
                    catch (Exception ex)
                    {
                        vru.Log.WriteError("", ex.Message);
                        vru.Log.WriteError("", ex.StackTrace);
                    }
                    return;
                    /*
                    LayFlat lf = (LayFlat)vru.Products[Guid.Parse("8cae3d9e-1824-4e81-bcda-b890f569eeb9")];
                    var lf = new LayFlat("7B", "") { UID = Guid.NewGuid() };
                    vru.Products.Add(lf);
                    vru.ProductKeys.Add(lf.UID);

                    lf.Parts = new List<Detail>();
                    lf.Parts.Add(new BlockDetail() { LayoutType = DetailLayoutTypeEnum.Spread});
                    lf.Parts.Add(new CoverDetail() { LayoutType = DetailLayoutTypeEnum.Lainer});

                    Layout lay = null;
                    lf.LayoutSpace.Add(lay = new Layout() { ProductSize = new Size(200, 280)});

                    Template t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, new SizeF(400f, 280f));
                    t.BindingFilter = BindingEnum.LayFlat;
                    t.DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert;

                    t.Frames = new SpreadFrameCollection(1, 1, 2, 2, t);
                    t.Frames.Add(new RectangleF(20f, 50f, 160f, 230f));
                    t.Frames.SetSide(PageSide.Right);
                    t.Frames.Add(new RectangleF(213f, 170f, 76f, 104f));
                    t.Frames.Add(new RectangleF(292f, 170f, 95f, 104f));
                    t.Frames.Add(new RectangleF(213f, 13f, 174f, 146f));
                    lay.TemplateCollection = new TemplateCollection(lay);
                    lay.TemplateCollection.Add(t);
                    */

                    /*
                    Layout lay = lf.LayoutSpace[size];
                    Template tt = lay.TemplateCollection[Guid.Parse("493ab2b9-88a8-40a5-b41f-063afb206297")];

                    tt.Save();
                    */


                    /*
                    Template t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultTwoPage, new SizeF(400f, 280f));
                    t.BindingFilter = BindingEnum.LayFlat;
                    t.DetailFilter = DetailTypeEnum.Block | DetailTypeEnum.Insert;

                    t.Frames = new SpreadFrameCollection(1, 1, 2, 2, t);
                    t.Frames.Add(new RectangleF(20f, 50f, 160f, 230f));
                    t.Frames.SetSide(PageSide.Right);
                    t.Frames.Add(new RectangleF(213f, 170f, 76f, 104f));
                    t.Frames.Add(new RectangleF(292f, 170f, 95f, 104f));
                    t.Frames.Add(new RectangleF(213f, 13f, 174f, 146f));
                    lay.TemplateCollection = new TemplateCollection(lay);
                    lay.TemplateCollection.Add(t);

                    t.Texts = new TextCollection(t);
                    TextFrame to = new TextFrame("КОВАЛЕНКО\r\nВИКТОРИЯ");
                    to.Bounds = new Fill(22, 13, 75, 27);
                    t.Texts.Add(to);

                    lf.TemplateKeys.Add(t.UID);
                    t.Save();
                    lf.Save();
                */
                    //Work.SaveXml(Path.Combine(WorkSpace.ApplicationPath, "config.xml"));


                    //mpp.RIP.ImageProcessor.DetectAnyObjects(x => Path.Combine(project.ProjectPath, "~Files", x.GUID + ".jpg"));


                    //Template.ExcludeFilter = null;
                    //List<KeyValuePair<string, List<string>>> extracted = new List<KeyValuePair<string, List<string>>>();
                    //FileProcess.ExtractFiles(tempPath, extracted, prj.FileTypeFilter);
                    //prj.ExtractData(extracted);


                    /*
                    FileItemComparer comparer = new FileItemComparer(prj);
                    var filesCluster = prj.Clusters.Single(x => x.SegmentType == SegmentTypeEnum.FileStructure);

                    for (int dirId = 0; dirId < filesCluster.TreeNodeItems.Count; dirId++)
                    {
                        for (int pardId = 0; pardId < filesCluster.TreeNodeItems[dirId].TreeNodeItems.Count; pardId++)
                        {
                            List<int> ind = new List<int>(filesCluster.TreeNodeItems[dirId].TreeNodeItems[pardId].Data);

                            for (int i = 0; i < ind.Count - 1; i++)
                            {
                                for (int j = i + 1; j < ind.Count; j++)
                                {
                                    comparer.Compare(ind[i], ind[j]);
                                }
                            }
                        }

                    }

                    foreach (var seg in prj.Clusters)
                    {
                        if (seg.SegmentType == SegmentTypeEnum.Directory)
                        { 
                            foreach (var files in seg)
                            {
                                List<int> ind = new List<int>(files.Data);
                                int counter = 0;
                                for (int i = 0; i < ind.Count - 1; i++)
                                {
                                    for (int j = i + 1; j < ind.Count; j++)
                                    {
                                        counter++;
                                        //StreamLogger.WriteLine($"{counter}: Dir = {files.KeyId}; i = {ind[i]}; j = {ind[j]}");
                                        comparer.Compare(ind[i], ind[j]);
                                    }
                                }
                            }
                        }
                    }
                    */
                    /*
                    var mat = prj.ProcessData();

                    StreamLogger.WriteLine(" >>>>>>>>>>>>>>>>>>>>");
                    StreamLogger.WriteLine("");
                    for (int i = 0; i < mat.GetLength(0); i++)
                    {
                        for (int j = 0; j < mat.GetLength(1); j++)
                        {
                            StreamLogger.Write($"{mat[i, j].ToString("000")}  ");
                        }
                        StreamLogger.WriteLine("");
                    }
                    */



                    //StreamLogger.WriteLine($"   ***************************************************************************");
                    //foreach (var pair in prj.InputData)
                    //{
                    //    StreamLogger.WriteLine($"   File = {pair.FileName}; Data = {pair.CreateImageMinutes.ToString()}; GPS = ({pair.GpsPosition.Item1.ToString()};{pair.GpsPosition.Item2.ToString()})");
                    //}
                    //Files.ClearDir(mInPath);
                    //mpp.CreateTempletesPreview(@"c:\Temp\templates.pdf");
                    //Template t = new Template(DetailLayoutTypeEnum.Spread, PageSide.DefaultPage, 400f, 280f);
                    //t.CreateFramesPreview(@"c:\Temp\frames.pdf");
                }

                Unlock();
            }
        }
        protected override void OnStart(string[] args)
        {
            //Work = (WorkSpace)Serializer.LoadXml(Path.Combine(WorkSpace.ApplicationPath, "config.xml"));


            mInPath = Path.Combine(WorkSpace.ApplicationPath, "works", "in");
            mOutPath = Path.Combine(WorkSpace.ApplicationPath, "works", "out");

            if (!Directory.Exists(mInPath))
            {
                Directory.CreateDirectory(mInPath);
            }
            if (!Directory.Exists(mOutPath))
            {
                Directory.CreateDirectory(mOutPath);
            }

            mTimer = new Timer(MainEventHandler, this, 0, 5000);

            WorkSpace.SystemLog.Open(Path.Combine(WorkSpace.ApplicationPath, "log.txt"));
            WorkSpace.SystemLog.WriteInfo("Сервис", "Процессы активированы");
        }
        protected override void OnStop()
        {
            mTimer.Dispose();
            WorkSpace.SystemLog.WriteInfo("Сервис", "Процессы остановлены");
            WorkSpace.SystemLog.Close();
        }
    }
}
