using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartproj.Utils
{
    public class ExifTaggedFileSegments : Segment
    {
        public ExifTaggedFileSegments(Segment _owner, string _key, SegmentTypeEnum _type, ICollection<int> _items = null) : base(_owner, _key, _type, _items)
        {
        }
        public ExifTaggedFileSegments() : base(null, "Root", SegmentTypeEnum.Root, null)
        {
        }
        protected override Segment CreateNew(Segment _owner, SegmentTypeEnum _type, string _key)
        {
            switch (_type)
            {
                case SegmentTypeEnum.Root:
                    return new ExifTaggedFileSegments(null, "Root", SegmentTypeEnum.Root);
                case SegmentTypeEnum.FileStructure:
                case SegmentTypeEnum.Directory:
                case SegmentTypeEnum.DateCreate:
                case SegmentTypeEnum.Geolocation:
                    return new ExifTaggedFileSegments(_owner, _key, _type);
                case SegmentTypeEnum.Segment:
                    if (_owner.SegmentType == SegmentTypeEnum.Directory)
                    {
                        return new ExifTaggedFileSegments(_owner, _key, _type);
                    }
                    else
                    {
                        return new ExifTaggedFileSegments(_owner, _key, _type, new HashSet<int>());
                    }
                default:
                    return null;
            }
        }
        public override Segment Add(SegmentTypeEnum _type, string _key, IEnumerable<int> _items = null)
        {
            try
            {
                if (SegmentType == SegmentTypeEnum.Segment)
                {
                    throw new InvalidOperationException($"Нельзя добавить вложенный уровень в сегмент данных");
                }
                if (_type == SegmentTypeEnum.Root)
                {
                    throw new InvalidOperationException($"Нельзя добавить корень в коллекцию");
                }
                if (SegmentType == SegmentTypeEnum.Root || SegmentType == SegmentTypeEnum.FileStructure)
                {
                    if (_type == SegmentTypeEnum.Segment) throw new InvalidOperationException($"Нельзя добавить сегмент данных напрямую в коллекцию '{SegmentType.ToString()}'");
                }
                else
                {
                    if (_type != SegmentTypeEnum.Segment) throw new InvalidOperationException($"В сегмент '{SegmentType.ToString()}' можно добавлять только сегмент данных");
                }
                if (SegmentType != SegmentTypeEnum.FileStructure && _type == SegmentTypeEnum.Directory)
                {
                    throw new InvalidOperationException($"Структуту '{_type.ToString()}' можно добавлять только в сегмент 'FileStructure'");
                }
                if (SegmentType != SegmentTypeEnum.Root && _type == SegmentTypeEnum.FileStructure)
                {
                    throw new InvalidOperationException($"Структуту 'FileStructure' можно добавлять только в корневой сегмент");
                }
            }
            catch (Exception e)
            {
                throw;
            }

            return base.Add(_type, _key, _items);
        }
        public void ImportFilesTree<T>(IList<T> _data, Func<T, string> _dirselector, Func<T, string> _nameselector, Func<T, int> _indexselector)
        {
            Segment fileSystem = Add(SegmentTypeEnum.FileStructure, "FileStructure");

            var dirs = _data.GroupBy(x => _dirselector(x));

            foreach (var dir in dirs)
            {
                ImportFiles(dir.Key, dir, _nameselector, _indexselector);
            }
        }
        public void ImportFiles<T>(string _dirName, IEnumerable<T> _files, Func<T, string> _nameselector, Func<T, int> _indexselector)
        {
            var dirContent = _files.GetGroupsSorted(_nameselector, _indexselector);
            ImportData(dirContent, SegmentTypeEnum.Directory, _dirName);
        }
        public void ImportKmeansClusters<T>(IEnumerable<IDistance<T>> _points, SegmentTypeEnum _type, string _key, T _maxsigma) where T : IComparable<T>
        {
            List<KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>> kmeans = null;

            if (_points.KMeans(_maxsigma, out kmeans, 1000))
            {
                ImportData(kmeans.Where(x => x.Value.Count > 0).Select(x => new KeyValuePair<string, IEnumerable<int>>(String.Join(";", x.Key.Points), x.Value.Select(y => y.Index))), _type, _key);
            }
            else
            {
                ImportData(null, _type, _key);
            }
        }
    }
}
