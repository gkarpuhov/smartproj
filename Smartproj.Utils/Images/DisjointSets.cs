using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Smartproj.Utils
{
    public class ImageSegment : IEnumerable<Point>
    {
        public ImageSegment(Point _first)
        {
            mPixels = new List<Point>();
            Add(_first);
        }
        public ImageSegment()
        {
            mPixels = new List<Point>();
        }
        private int X1 = int.MaxValue;
        private int Y1 = int.MaxValue;
        private int X2 = -1;
        private int Y2 = -1;
        public int Count => mPixels.Count;
        public Rectangle Frame => new Rectangle(X1, Y1, X2 - X1 + 1, Y2 - Y1 + 1);
        public float FillFactor { get { return mPixels.Count > 0 ? (float)mPixels.Count / (float)((X2 - X1 + 1) * (Y2 - Y1 + 1)) : 0; } }
        private List<Point> mPixels;
        public void Add(Point _item)
        {
            mPixels.Add(_item);
            if (_item.X < X1) X1 = _item.X;
            if (_item.X > X2) X2 = _item.X;
            if (_item.Y < Y1) Y1 = _item.Y;
            if (_item.Y > Y2) Y2 = _item.Y;
        }
        public IEnumerator<Point> GetEnumerator()
        {
            return mPixels.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mPixels.GetEnumerator();
        }
    }
    public class DisjointSets
    {
        private readonly Dictionary<Point, int> mItemToIndex = new Dictionary<Point, int>();
        private readonly List<int> mParent = new List<int>();
        private readonly List<int> mRank = new List<int>();
        /// <summary>
        /// Поиск номера корневого элемента множества для указанного элемента
        /// </summary>
        private int FindSet(Point _item)
        {
            if (!mItemToIndex.TryGetValue(_item, out int index))
            {
                // Элемента не было, создаём новое множество для него
                index = mParent.Count;
                mItemToIndex.Add(_item, index);
                mParent.Add(index);
                mRank.Add(0);
                return index;
            }

            // Поиск корня в множестве 
            int set;
            for (set = index; mParent[set] != set; set = mParent[set]);

            // Эвристика сокращения путей
            while (index != set)
            {
                var p = mParent[index];
                mParent[index] = set;
                index = p;
            }

            return set;
        }
        public Dictionary<int, ImageSegment> GetData()
        {
            Dictionary<int, ImageSegment> ret = new Dictionary<int, ImageSegment>();
            foreach (var indexed in mItemToIndex)
            {
                if (!ret.TryGetValue(mParent[indexed.Value], out ImageSegment segment))
                {
                    ret.Add(mParent[indexed.Value], new ImageSegment(indexed.Key));
                }
                else
                {
                    segment.Add(indexed.Key);
                }
            }

            return ret;
        }
        /// <summary>
        /// Определяет лежат ли два элемента в одном множестве
        /// </summary>
        public bool IsInSameSet(Point _item1, Point _item2) => FindSet(_item1) == FindSet(_item2);
        public bool IsInSameSet(Point _item1, Point _item2, out int _set1, out int _set2)
        {
            _set1 = FindSet(_item1);
            _set2 = FindSet(_item2);
            return _set1 == _set2;
        }
        /// <summary>
        /// Объединяет два множества
        /// </summary>
        public void Union(Point _item1, Point _item2)
        {
            var set1 = FindSet(_item1);
            var set2 = FindSet(_item2);

            if (set1 == set2) return;

            // Эвристика рангов
            if (mRank[set1] > mRank[set2])
            {
                mParent[set2] = set1;
            }
            else
            {
                if (mRank[set1] < mRank[set2])
                {
                    mParent[set1] = set2;
                }
                else
                {
                    mParent[set1] = set2;
                    mRank[set2]++;
                }
            }
        }
        public void Union(int _set1, int _set2)
        {
            var set1 = _set1;
            var set2 = _set2;

            if (set1 == set2) return;

            // Эвристика рангов
            if (mRank[set1] > mRank[set2])
            {
                mParent[set2] = set1;
            }
            else
            {
                if (mRank[set1] < mRank[set2])
                {
                    mParent[set1] = set2;
                }
                else
                {
                    mParent[set1] = set2;
                    mRank[set2]++;
                }
            }
        }
    }
}
