using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Smartproj.Utils
{
    public static class TypeEx
    {
        private static Type[] ExConvertersFrom = new Type[] { typeof(Rectangle), typeof(RectangleF), typeof(byte[])};
        private static Type[] ExConvertersTo = new Type[] { typeof(byte[]) };
        public static bool CanConvertFromString(this Type _type) => ExConvertersFrom.Contains(_type);
        public static bool CanConvertToString(this Type _type) => ExConvertersTo.Contains(_type);
        public static string ContertToString(this Type _type, object _value)
        {
            if (_value != null)
            {
                if (_type == typeof(byte[]))
                {
                    return String.Join(" ", (byte[])_value);
                }
            }
            return null;
        }
        public static object ContertFromString(this Type _type, string _value)
        {
            if (_type != null && _value != null && CanConvertFromString(_type))
            {
                if (_type == typeof(Color))
                {
                    Match match = Regex.Match(_value, @"(^[0-2]|^)([0-2]?[0-5]?[0-5])[\;\,\s]+([0-2]?[0-5]?[0-5])[\;\,\s]+([0-2]?[0-5]?[0-5])[\;\,\s]+([0-2]?[0-5]?[0-5])(^[0-5]|$)", RegexOptions.Compiled);
                    if (match.Success)
                    {
                        return Color.FromArgb(byte.Parse(match.Groups[2].Value), byte.Parse(match.Groups[3].Value), byte.Parse(match.Groups[4].Value), byte.Parse(match.Groups[5].Value));
                    }
                }
                if (_type == typeof(RectangleF))
                {
                    Match match = Regex.Match(_value, @"(X=)?([\d\.]+)[\s,;]+(Y=)?([\d\.]+)[\s,;]+(Width=)?([\d\.]+)[\s,;]+(Height=)?([\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success) 
                    {
                        float x = float.Parse(match.Groups[2].Value, NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        float y = float.Parse(match.Groups[4].Value, NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        float w = float.Parse(match.Groups[6].Value, NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        float h = float.Parse(match.Groups[8].Value, NumberStyles.Float, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                        return new RectangleF(x, y, w, h);
                    }
                }
                if (_type == typeof(Rectangle))
                {
                    Match match = Regex.Match(_value, @"(X=)?(\d+)[\s,;]+(Y=)?(\d+)[\s,;]+(Width=)?(\d+)[\s,;]+(Height=)?(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success)
                    {
                        int x = int.Parse(match.Groups[2].Value);
                        int y = int.Parse(match.Groups[4].Value);
                        int w = int.Parse(match.Groups[6].Value);
                        int h = int.Parse(match.Groups[8].Value);
                        return new Rectangle(x, y, w, h);
                    }
                }
                if (_type == typeof(byte[]))
                {
                    MatchCollection matches = Regex.Matches(_value, @"(\d+)([\,\;\s]+|$)");
                    List<byte> bytes = new List<byte>(matches.Count);    
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches) 
                        {
                            bytes.Add(byte.Parse(match.Groups[1].Value));
                        }
                    }
                    return bytes.ToArray();    
                }
            }

            return null;
        }
    }
}
