using System;
using System.Collections;
using System.Collections.Generic;

namespace Smartproj.Utils
{
    public class Link<T> : IEnumerable<T>   
    {
        private T mData;
        private Link<T> mNext;
        public Link<T> Find(T _data, Func<T, T,bool> _comparison)
        {
            if (_comparison(_data, mData)) return this;

            Link<T> next = mNext;
            while (next != null && next != this)
            {
                if (_comparison(_data, next.mData))
                {
                    return next;
                }
                next = next.mNext;
            }

            return null;
        }
        public T Value 
        { 
            get { return mData; } 
            set { mData = value; }
        }
        public Link<T> Next
        {
            get { return mNext; }
            set { mNext = value; }
        }
        public Link(T _data)
        {
            mData = _data;
        }
        public IEnumerable<Link<T>> GetItems()
        {
            return new AnyEnumerable<Link<T>>(GetItemsIEnumerator());
        }
        private IEnumerator<Link<T>> GetItemsIEnumerator()
        {
            yield return this;
            Link<T> next = mNext;
            while (next != null && next != this)
            {
                var data = next;
                next = next.mNext;
                yield return data;
            }
        }
        public IEnumerator<T> GetEnumerator()
        {
            yield return mData;
            Link<T> next = mNext;
            while (next != null && next != this)
            {
                var data = next.mData;
                next = next.mNext;
                yield return data;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
