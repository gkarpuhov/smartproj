using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smartproj.Utils
{
    public static class StringEx
    {
        public static int StringHashCode40(this string _value)
        {
            int num = 5381;
            int num2 = num;

            if (_value != null)
            {
                for (int i = 0; i < _value.Length; i += 2)
                {
                    num = (((num << 5) + num) ^ _value[i]);

                    if (i + 1 < _value.Length)
                    {
                        num2 = (((num2 << 5) + num2) ^ _value[i + 1]);
                    }
                }
            }
            return num + num2 * 1566083941;
        }
        public static IDictionary<string, IEnumerable<TOut>> GetGroupsSorted<T, TOut>(this IEnumerable<T> _strings, Func<T, string> _keyselector, Func<T, TOut> _selector)
        {
            IDictionary<string, IEnumerable<TOut>> data = new SortedDictionary<string, IEnumerable<TOut>>();

            var list_0 = _strings.GroupBy(x =>
            {
                var matches = Regex.Matches(_keyselector(x), @"(^|[^\d]+)?(\d+)($|[^\d]+)", RegexOptions.Compiled);
                return matches.Count > 0 && matches.Count < 5 ? $"{matches[0].Groups[1].Value}*{matches.Count.ToString("0000")}" : "*";
            });

            foreach (var item in list_0)
            {
                string key = item.Key;
                IEnumerable<TOut> group = null;
                // Если в цифровой группе только одна позиция, считаем как будто это отдельная строка
                if (key != "*" && item.Count() == 1)
                {
                    key = "*";
                }
                if (key == "*")
                {
                    data.TryGetValue(key, out group);
                }
                if (group == null)
                {
                    group = new List<TOut>();
                    data.Add(key, group);
                }
                IEnumerable<T> sorted = null;
                if (key == "*")
                {
                    sorted = item.OrderBy(x => _keyselector(x));
                }
                else
                {
                    sorted = item.OrderBy(x => Regex.Replace(_keyselector(x), @"(^|[^\d]+)?(\d+)($|[^\d]+)", match => $"{match.Groups[1].Value}{match.Groups[2].Value.PadLeft(9, '0')}{match.Groups[3].Value}", RegexOptions.Compiled));
                }

                ((List<TOut>)group).AddRange(sorted.Select(x => _selector(x)));
            }

            return data;
        }
        public static IDictionary<string, IEnumerable<TOut>> GetGroupsSorted<T, TOut>(this IEnumerable<T> _strings, Func<T, TOut> _selector) where T : IKeyd<string>
        {
            IDictionary<string, IEnumerable<TOut>> data = new Dictionary<string, IEnumerable<TOut>>();

            var list_0 = _strings.GroupBy(x =>
            {
                var matches = Regex.Matches(x.KeyId, @"(^|[^\d]+)?(\d+)($|[^\d]+)", RegexOptions.Compiled);
                return matches.Count > 0 && matches.Count < 5 ? $"{matches[0].Groups[1].Value}*{matches.Count.ToString("0000")}" : "*";
            });

            foreach (var item in list_0)
            {
                string key = item.Key;
                IEnumerable<TOut> group = null;
                // Если в цифровой группе только одна позиция, считаем как будто это отдельная строка
                if (key != "*" && item.Count() == 1)
                {
                    key = "*";
                }
                if (key == "*")
                {
                    data.TryGetValue(key, out group);
                }
                if (group == null)
                {
                    group = new List<TOut>();
                    data.Add(key, group);
                }
                IEnumerable<T> sorted = null;
                if (key == "*")
                {
                    sorted = item.OrderBy(x => x.KeyId);
                }
                else
                {
                    sorted = item.OrderBy(x => Regex.Replace(x.KeyId, @"(^|[^\d]+)?(\d+)($|[^\d]+)", match => $"{match.Groups[1].Value}{match.Groups[2].Value.PadLeft(5, '0')}{match.Groups[3].Value}", RegexOptions.Compiled));
                }
                List<TOut> grouplist = (List<TOut>)group;
                grouplist.AddRange(sorted.Select(x => _selector(x)));
            }

            return data;
        }
        public static TagMIMETypeEnum ToMIMEType(this string _string)
        {
            Match match = Regex.Match(_string, @"^\s*([^\/]+)\/([^\.]+\.)*(x-canon-cr2|photoshop|p?jpeg|tiff|bmp|heic|png|pdf|plain|xml|json|postscript|g?zip|octet-stream|gif|webp|msword|ms-excel|html|iccprofile)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            if (match.Success)
            {
                if (match.Groups[1].Value.ToLower() == "application")
                {
                    switch (match.Groups[3].Value.ToLower())
                    {
                        case "postscript":
                            return TagMIMETypeEnum.POSTSCRIPT;
                        case "gzip":
                        case "zip":
                            return TagMIMETypeEnum.ZIP;
                        case "pdf":
                            return TagMIMETypeEnum.PDF;
                        case "json":
                            return TagMIMETypeEnum.JSON;
                        case "xml":
                            return TagMIMETypeEnum.XML;
                        case "msword":
                            return TagMIMETypeEnum.DOC;
                        case "ms-excel":
                            return TagMIMETypeEnum.EXCEL;
                        case "octet-stream":
                            return TagMIMETypeEnum.BIN;
                        case "iccprofile":
                            return TagMIMETypeEnum.ICC;
                    }
                }
                if (match.Groups[1].Value.ToLower() == "image")
                {
                    switch (match.Groups[3].Value.ToLower())
                    {
                        case "gif":
                            return TagMIMETypeEnum.GIF;
                        case "bmp":
                            return TagMIMETypeEnum.BMP;
                        case "jpeg":
                        case "pjpeg":
                            return TagMIMETypeEnum.JPEG;
                        case "tiff":
                            return TagMIMETypeEnum.TIFF;
                        case "heic":
                            return TagMIMETypeEnum.HEIC;
                        case "png":
                            return TagMIMETypeEnum.PNG;
                        case "webp":
                            return TagMIMETypeEnum.WEBP;
                        case "x-canon-cr2":
                            return TagMIMETypeEnum.CR2;
                        case "photoshop":
                            return TagMIMETypeEnum.PSD;
                    }
                }
                if (match.Groups[1].Value.ToLower() == "text")
                {
                    switch (match.Groups[3].Value.ToLower())
                    {
                        case "plain":
                            return TagMIMETypeEnum.TEXT;
                        case "xml":
                            return TagMIMETypeEnum.XML;
                        case "html":
                            return TagMIMETypeEnum.HTML;
                    }
                }
            }
            return TagMIMETypeEnum.UNDEFINED;
        }
        public static TagFileTypeEnum ToFileType(this string _string)
        {
            Match match = Regex.Match(_string, @"\.?(cr2|psd|jpe?g|tiff?|bmp|heic|png|gif|jfif|webp|pdf|ps|eps|g?zip|txt|xml|json|html|doc|xlsx|icc|icm|dll|exe)\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.RightToLeft);

            if (match.Success)
            {
                switch (match.Groups[1].Value.ToLower())
                {
                    case "cr2":
                        return TagFileTypeEnum.CR2;
                    case "psd":
                        return TagFileTypeEnum.PSD;
                    case "jfif":
                        return TagFileTypeEnum.JFIF;
                    case "jpeg":
                    case "jpg":
                        return TagFileTypeEnum.JPEG;
                    case "tiff":
                    case "tif":
                        return TagFileTypeEnum.TIFF;
                    case "bmp":
                        return TagFileTypeEnum.BMP;
                    case "heic":
                        return TagFileTypeEnum.HEIC;
                    case "png":
                        return TagFileTypeEnum.PNG;
                    case "gif":
                        return TagFileTypeEnum.GIF;
                    case "webp":
                        return TagFileTypeEnum.WEBP;
                    case "pdf":
                        return TagFileTypeEnum.PDF;
                    case "ps":
                        return TagFileTypeEnum.PS;
                    case "eps":
                        return TagFileTypeEnum.EPS;
                    case "zip":
                    case "gzip":
                        return TagFileTypeEnum.ZIP;
                    case "txt":
                        return TagFileTypeEnum.TEXT;
                    case "xml":
                        return TagFileTypeEnum.XML;
                    case "json":
                        return TagFileTypeEnum.JSON;
                    case "html":
                        return TagFileTypeEnum.HTML;
                    case "doc":
                        return TagFileTypeEnum.DOC;
                    case "xlsx":
                        return TagFileTypeEnum.EXCEL;
                    case "icc":
                    case "icm":
                        return TagFileTypeEnum.ICC;
                    case "dll":
                        return TagFileTypeEnum.DLL;
                    case "exe":
                        return TagFileTypeEnum.EXE;
                }
            }
            return TagFileTypeEnum.UNDEFINED;
        }
    }

}
