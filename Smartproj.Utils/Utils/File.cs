using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smartproj.Utils
{
    public static class FileProcess
    {
        public static void ExtractFiles(string _dirName, List<KeyValuePair<string, List<string>>> _list)
        {
            ExtractFiles(_dirName, _list, TagFileTypeEnum.ANYFILES, true, false);
        }
        public static void ExtractFiles(string _dirName, List<KeyValuePair<string, List<string>>> _list, TagFileTypeEnum _filter)
        {
            ExtractFiles(_dirName, _list, _filter, true, true);
        }
        public static void ExtractFiles(string _dirName, List<KeyValuePair<string, List<string>>> _list, TagFileTypeEnum _filter, bool _recursive, bool _extractZip)
        {
            if (_list == null)
            {
                _list = new List<KeyValuePair<string, List<string>>>();
            }
            if (_dirName == null || _dirName == "" || !Directory.Exists(_dirName)) return;

            var localfiles = Directory.GetFiles(_dirName, "*.*", SearchOption.TopDirectoryOnly);
            if (localfiles.Any(f => Path.GetFileName(f)[0] != '.'))
            {
                KeyValuePair<string, List<string>> item = new KeyValuePair<string, List<string>>(_dirName, new List<string>());
                foreach (string localfile in localfiles)
                {
                    string filename = Path.GetFileName(localfile);
                    if (filename[0] != '.')
                    {
                        TagFileTypeEnum type = filename.ToFileType();
                        if (Path.GetExtension(localfile) != ".zip" || !_extractZip)
                        {
                            if ((_filter & type) == type || type == TagFileTypeEnum.UNDEFINED || _filter == TagFileTypeEnum.ANYFILES) item.Value.Add(Path.GetFileName(localfile));
                        }
                        else
                        {
                            string zipDir = Path.Combine(_dirName, Path.GetFileNameWithoutExtension(localfile));
                            Directory.CreateDirectory(zipDir);
                            ZipFile.ExtractToDirectory(localfile, zipDir);
                            File.Delete(localfile);
                        }
                    }
                }
                if (item.Value.Count > 0)
                {
                    _list.Add(item);
                }
            }

            if (!_recursive) return;

            var localdirs = Directory.GetDirectories(_dirName, "*", SearchOption.TopDirectoryOnly);
            if (localdirs.Any(f => Path.GetFileName(f)[0] != '.'))
            {
                //foreach (string localdir in localdirs.OrderBy(x => Regex.Replace(Path.GetFileName(x), @"(^|[^\d]+)?(\d+)($|[^\d]+)", match => $"{match.Groups[1].Value}{match.Groups[2].Value.PadLeft(5, '0')}{match.Groups[3].Value}", RegexOptions.Compiled)))
                foreach (string localdir in localdirs)
                {
                    if (Path.GetFileName(localdir)[0] != '.')
                    {
                        ExtractFiles(localdir, _list, _filter, _recursive, _extractZip);
                    }
                }
            }
        }
        public static void ClearDir(string _path)
        {
            string[] directories = Directory.GetDirectories(_path, "*", SearchOption.TopDirectoryOnly);
            foreach (var current in directories)
            {
                FindAndDeleteEmptyDirectory(current);
            }
        }
        public static bool FindAndDeleteEmptyDirectory(string _dirName)
        {
            bool subDeleted = true;
            if (Path.GetFileName(_dirName)[0] != '.')
            {
                string[] directories = Directory.GetDirectories(_dirName, "*", SearchOption.TopDirectoryOnly);
                foreach (var current in directories)
                {
                    if (!FindAndDeleteEmptyDirectory(current)) subDeleted = false;
                }
            }

            string[] files = Directory.GetFiles(_dirName, "*.*", SearchOption.TopDirectoryOnly);
            if ((subDeleted && files.SkipWhile(f => Path.GetFileName(f)[0] == '.').Count() == 0) || Path.GetFileName(_dirName)[0] == '.')
            {
                try
                {
                    Directory.Delete(_dirName, true);
                    return true;
                }
                catch (Exception)
                { }
            }

            return false;
        }
        public static bool CheckForProcessFile(IEnumerable<string> _items)
        {
            bool checkAccess = true;
            foreach (var item in _items)
            {
                if (CheckForProcessFile(item) == -1)
                {
                    checkAccess = false;
                    break;
                }
            }
            return checkAccess;
        }
        public static long CheckForProcessFile(string _filename, long _minSize = 0)
        {
            FileStream stream = null;
            if (TryGetFileStream(_filename, FileAccess.Read, FileShare.ReadWrite, out stream))
            {
                long size = stream.Length;
                stream.Close();
                if (size >= _minSize || Path.GetFileName(_filename)[0] == '.')
                {
                    return size;
                }
                else
                    return -1;
            }
            return -1;
        }
        public static bool TryGetFileStream(string _filename, FileAccess _sccess, FileShare _share, out FileStream _stream)
        {
            _stream = null;
            try
            {
                _stream = new FileStream(_filename, FileMode.Open, _sccess, _share);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
