using Smartproj.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace Smartproj.Utils
{
    public abstract class Segment : IKeyd<string>, ITree, IEnumerable<Segment>
    {
        private Segment mRoot;
        private Segment mParent;
        private int mLevel;
        private bool mIsRoot;
        private List<Segment> mSegments;
        private ICollection<int> mData;
        public event EventHandler NodeAdding;
        //
        public SegmentTypeEnum SegmentType { get; }
        public ICollection<int> Data
        {
            get { return mData; }
            set { mData = value; }
        }
        public string KeyId { get; }
        public int Index { get; private set; }
        public Segment Root => mRoot;
        public Segment Parent => mParent;
        public int Level => mLevel;
        public bool IsRoot => mIsRoot;
        public IList<Segment> ChildNodes => mSegments.AsReadOnly();
        public int Degree => mSegments.Count;
        public Segment this[SegmentTypeEnum _type]
        {
            get
            {
                for (int i = 0; i < mSegments.Count; i++)
                {
                    if (mSegments[i].SegmentType == _type) return mSegments[i];
                }
                return null;
            }
        }
        protected Segment(Segment _owner, string _key, SegmentTypeEnum _type, ICollection<int> _items = null)
        {
            KeyId = _key;
            SegmentType = _type;
            mIsRoot = true;
            mLevel = 0;
            mRoot = this;
            mParent = _owner;
            mSegments = new List<Segment>();
            mData = _items;

            if (mData == null)
            {
                mData = new List<int>();
            }
        }
        protected virtual void InsertItem(int _index, Segment _parent, Segment _item)
        {
            _item.onNodeAdding(this);

            _item.mParent = _parent;
            _item.mIsRoot = false;
            _item.ApplyForAll(x =>
            {
                ((Segment)x).mRoot = _parent.mRoot;
                ((Segment)x).mLevel = x.Parent.Level + 1;
            });

            if (_index < 0)
                _parent.mSegments.Add(_item);
            else
                _parent.mSegments.Insert(_index, _item);
        }
        protected virtual void Insert(int _index, Segment _child)
        {
            if (mRoot == _child.mRoot)
            {
                throw new ArgumentException("Узел уже находиться в дереве");
            }
            mRoot.InsertItem(_index, this, _child);
        }
        //
        ITree ITree.Parent => Parent;
        ITree ITree.Root => Root;
        int IKeyd<string>.OrderBy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ITree ITree.GetChild(int _index)
        {
            return mSegments[_index];
        }
        void ITree.Add(ITree _child)
        {
            Insert(-1, (Segment)_child);
        }
        bool ITree.Remove(ITree _child)
        {
            throw new NotImplementedException();
        }
        protected virtual void onNodeAdding(Segment _node)
        {
            if (NodeAdding != null) NodeAdding(_node, EventArgs.Empty);
        }
        protected abstract Segment CreateNew(Segment _owner, SegmentTypeEnum _type, string _key);
        public Segment Add(string _key)
        {
            return Add(SegmentTypeEnum.Segment, _key);
        }
        public virtual Segment Add(SegmentTypeEnum _type, string _key, IEnumerable<int> _items = null)
        {
            Segment segment = null;
            for (int i = 0; i < mSegments.Count; i++)
            {
                if (_key == mSegments[i].KeyId && _type == mSegments[i].SegmentType)
                {
                    segment = mSegments[i];
                    break;
                }
            }
            if (segment == null)
            {
                segment = CreateNew(this, _type, _key);
                segment.Index = mSegments.Count;
                Insert(-1, segment);
            }
            if (_items != null)
            {
                foreach (var item in _items)
                {
                    segment.mData.Add(item);
                }
            }
            return segment;
        }
        public void Clear() => Clear(false, false);
        public void Clear(bool _rersv, bool _items)
        {
            if (_items)
            {
                mData.Clear();
            }
            if (_rersv)
            {
                for (int i = 0; i < mSegments.Count; i++)
                {
                    mSegments[i].Clear(true, true);
                }
            }
            mSegments.Clear();
        }
        protected virtual void ImportData(IEnumerable<KeyValuePair<string, IEnumerable<int>>> _data, SegmentTypeEnum _type, string _key)
        {
            var segment = Add(_type, _key);

            if (segment.mData.Count > 0 || segment.mSegments.Count > 0)
            {
                segment.Clear(true, true);
            }
            if (_data != null && _data.Count() > 0)
            {
                foreach (var item in _data)
                {
                    segment.Add(SegmentTypeEnum.Segment, item.Key, item.Value);
                }
            }
            else
            {
                segment.Add(SegmentTypeEnum.Segment, "Empty");
            }
         }
        public IEnumerator<Segment> GetEnumerator()
        {
            return this.GetTreeEnumerator<Segment>();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetTreeEnumerator<Segment>();
        }
    }
}
