using Smartproj.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Smartproj
{
    public class Job : IDisposable
    {
        public Job(Project _project)
        {
            Owner = _project;
            mStatus = ProcessStatusEnum.New;
            mIsDisposed = false;
            UID = Guid.NewGuid();
            Clusters = new ExifTaggedFileSegments();
            DataContainer = new List<ExifTaggedFile>();
            JobPath = Path.Combine(Owner.ProjectPath, "Jobs", UID.ToString());
            Directory.CreateDirectory(JobPath);
        }
        private bool mIsDisposed;
        private object mSyncRoot = new Object();
        private ProcessStatusEnum mStatus;
        public ProcessStatusEnum Status { get { lock (mSyncRoot) { return mStatus; } } set { lock (mSyncRoot) { mStatus = value; } } }
        public Logger Log => Owner?.Log; 
        public Project Owner { get; }
        public string JobPath { get; private set; }
        public List<ExifTaggedFile> DataContainer { get; }
        public Segment Clusters { get; }
        public Product Product { get; private set; }
        public Guid UID { get; }
        public SourceParametersTypeEnum MetadataType { get; private set; }
        public TagFileTypeEnum FileDataFilter { get; private set; }
        public string Metadata { get; private set; }
        public virtual void Create(Product _product, Size _productSize, string _metadata, SourceParametersTypeEnum _metadataType, TagFileTypeEnum _fileDataFilter)
        {
            MetadataType = _metadataType;
            FileDataFilter = _fileDataFilter;
            Metadata = _metadata;
            Product = _product;
            Product.Owner = this;
            Product.CreateLayoutSpace(_productSize);
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
