using System.Collections;
using System.Collections.Generic;

namespace Smartproj.Utils
{
    public class AnyEnumerable<T> : IEnumerable<T>
    {
        public AnyEnumerable(IEnumerator<T> _enumerator)
        {
            mEnumerator = _enumerator;
        }

        private readonly IEnumerator<T> mEnumerator;
        public IEnumerator<T> GetEnumerator()
        {
            return mEnumerator;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mEnumerator;
        }
    }
    public class ListEnumerator<T> : IEnumerator<T>
    {
        public ListEnumerator(IList<T> _list)
        {
            mList = _list;
        }
        private IList<T> mList;
        private int index = -1;
        object IEnumerator.Current
        {
            get { return index != -1 ? mList[index] : default(T); }
        }
        public T Current
        {
            get { return index != -1 ? mList[index] : default(T); }
        }
        public bool MoveNext()
        {
            if (index < mList.Count - 1)
            {
                index++;
                return true;
            }
            else
                return false;
        }
        public void Reset()
        {
            index = -1;
        }
        public void Dispose()
        {
            index = -1;
        }
    }

}
