using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Xml.Serialization;

namespace Smartproj.Utils
{
    public interface ITree
    {
        int Degree { get; }
        int Level { get; }
        bool IsRoot { get; }
        ITree GetChild(int _index);
        ITree Parent { get; }
        ITree Root { get; }
        void Add(ITree _child);
        bool Remove(ITree _child);
        void Clear();
    }
    public class Tree : ITree, IKeyd<string>
    {
        private Tree mRoot;
        private Tree mParent;
        private int mLevel;
        private bool mIsRoot;
        private int mIndex;
        public Tree Root => mRoot;
        public Tree Parent => mParent;
        public bool IsRoot => mIsRoot;
        public event EventHandler NodeAdding;
        public virtual Tree this[int _index] => TreeNodeItems?[_index];
        public virtual Tree this[string _key] => TreeNodeItems?.Find(x => x.KeyId == _key);
        public virtual Logger Log => mParent?.Log;
        public virtual int OrderBy { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public int Index => mIndex;
        public int Level => mLevel;
        public int Degree => TreeNodeItems.Count;
        [XmlElement]
        public virtual string KeyId { get; set; }
        [XmlCollection(true, false, typeof(Tree))]
        protected List<Tree> TreeNodeItems { get; set; }
        public Tree()
        {
            mRoot = this;
            mIsRoot = true;
            mLevel = 0;
            mIndex = 0;
            TreeNodeItems = new List<Tree>();
        }
        protected virtual void InsertItem(int _index, Tree _parent, Tree _item)
        {
            _item.mParent = _parent;
            _item.mIsRoot = false;
            _item.ApplyForAll(x =>
            {
                ((Tree)x).mRoot = _parent.mRoot;
                ((Tree)x).mLevel = x.Parent.Level + 1;
            });

            if (_index < 0)
            {
                _item.mIndex = _parent.TreeNodeItems.Count;
                _parent.TreeNodeItems.Add(_item);
            }
            else
            {
                _parent.TreeNodeItems.Insert(_index, _item);
                for (int i = _index; i < _parent.TreeNodeItems.Count; i++)
                {
                    _parent.TreeNodeItems[i].mIndex = i;    
                }
            }
        }
        protected virtual void Insert(int _index, Tree _child)
        {
            if (mRoot == _child.mRoot)
            {
                throw new ArgumentException("Узел уже находиться в дереве");
            }
            mRoot.InsertItem(_index, this, _child);
        }
        public void Add(Tree _child)
        {
            Insert(-1, _child);
        }
        public void ClearTree(bool _rersv)
        {
            if (_rersv)
            {
                for (int i = 0; i < TreeNodeItems.Count; i++)
                {
                    TreeNodeItems[i].ClearTree(true);
                }
            }
            Clear();
        }
        public virtual void Clear()
        {
            for (int i = 0; i < TreeNodeItems.Count; i++)
            {
                TreeNodeItems[i].mRoot = null;
                TreeNodeItems[i].mParent = null;
                TreeNodeItems[i].mLevel = 0;
                TreeNodeItems[i].mIsRoot = true;
            }
            TreeNodeItems.Clear();
        }
        ITree ITree.Parent => Parent;
        ITree ITree.Root => Root;
        ITree ITree.GetChild(int _index)
        {
            return this[_index];
        }
        void ITree.Add(ITree _child)
        {
            Insert(-1, (Tree)_child);
        }
        void ITree.Clear()
        {
            Clear();
        }
        bool ITree.Remove(ITree _child)
        {
            throw new NotImplementedException();
        }
        protected virtual void onNodeAdding(Segment _node)
        {
            if (NodeAdding != null) NodeAdding(_node, EventArgs.Empty);
        }
    }
}
