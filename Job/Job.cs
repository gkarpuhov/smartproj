using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GdPicture14;
using Smartproj.Utils;

namespace Smartproj
{
    public class Job : IDisposable
    {
        public Job(Project _project)
        {
            Owner = _project;
            mIsDisposed = false;
            UID = Guid.NewGuid();
            Clusters = new ExifTaggedFileSegments();
            DataContainer = new List<ExifTaggedFile>();
            JobPath = Path.Combine(Owner.ProjectPath, UID.ToString());
            Directory.CreateDirectory(JobPath);
        }
        private bool mIsDisposed;
        public Logger Log => Owner?.Log; 
        public Project Owner { get; }
        public string JobPath { get; private set; }
        public List<ExifTaggedFile> DataContainer { get; }
        public Segment Clusters { get; }
        public Product Product { get; private set; }
        public Guid UID { get; }
        public virtual void Create(Product _product, Size _productSize)
        {
            string JobPath = Path.Combine(Owner.ProjectPath, UID.ToString());
            if (!Directory.Exists(JobPath))
            {
                Directory.CreateDirectory(JobPath);
            }

            Product = _product;
            Product.Owner = this;
            Product.CreateLayoutSpace(_productSize);
        }
        public bool Create(Guid _productId, Size _productSize)
        {
            string[] files = Directory.GetFiles(Path.Combine(Owner.Home, "Products"), "*.xml", SearchOption.AllDirectories);
            string productFile = files.SingleOrDefault(x => String.Compare(Path.GetFileNameWithoutExtension(x), _productId.ToString(), true) == 0);
            if (productFile != null)
            {
                try
                {
                    Create((Product)Serializer.LoadXml(productFile), _productSize);
                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteError("CreateProducts", $"Ошибка при загрузке продукта '{productFile}: {ex.Message}");
                    Log.WriteError("CreateProducts", $"Ошибка при загрузке продукта '{productFile}: {ex.StackTrace}");
                }
            }
            return false;
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
