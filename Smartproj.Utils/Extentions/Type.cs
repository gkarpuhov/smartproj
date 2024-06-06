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
        private static Type[] ExConvertersFrom = new Type[] { typeof(Rectangle), typeof(RectangleF), typeof(byte[]), typeof(float[]), typeof(PointF), typeof(float), typeof(double), typeof(Margins) };
        private static Type[] ExConvertersTo = new Type[] { typeof(byte[]), typeof(float[]), typeof(RectangleF), typeof(PointF), typeof(float), typeof(double), typeof(Margins) };
        public static bool CanConvertFromString(this Type _type) => ExConvertersFrom.Contains(_type);
        public static bool CanConvertToString(this Type _type) => ExConvertersTo.Contains(_type);
        public static string ContertToString(this Type _type, object _value)
        {
            if (_value != null)
            {
                NumberFormatInfo format = new NumberFormatInfo() { NumberDecimalSeparator = "." };

                if (_type == typeof(byte[]))
                {
                    return String.Join(" ", (byte[])_value);
                }
                if (_type == typeof(float[]))
                {
                    return String.Join(" ", ((float[])_value).Select(x => x.ToString(format)));
                }
                if (_type == typeof(RectangleF))
                {
                    RectangleF rect = (RectangleF)_value;
                    return $"{{X={rect.X.ToString(format)},Y={rect.Y.ToString(format)},Width={rect.Width.ToString(format)},Height={rect.Height.ToString(format)}}}";
                }
                if (_type == typeof(Margins))
                {
                    Margins margins = (Margins)_value;
                    return $"{{Top={margins.Top.ToString(format)},Left={margins.Left.ToString(format)},Bottom={margins.Bottom.ToString(format)},Right={margins.Right.ToString(format)}}}";
                }
                if (_type == typeof(PointF))
                {
                    PointF point = (PointF)_value;
                    
                    return $"{{X={point.X.ToString(format)},Y={point.Y.ToString(format)}}}";
                }
                if (_type == typeof(float))
                {
                    return ((float)_value).ToString(format);
                }
                if (_type == typeof(double))
                {
                    return ((double)_value).ToString(format);
                }
            }
            return null;
        }
        public static object ContertFromString(this Type _type, string _value)
        {
            if (_type != null && _value != null && CanConvertFromString(_type))
            {
                NumberFormatInfo format = new NumberFormatInfo() { NumberDecimalSeparator = "." };

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
                    Match match = Regex.Match(_value, @"(X=)?(-?[\d\.]+)[\s,;]+(Y=)?(-?[\d\.]+)[\s,;]+(Width=)?(-?[\d\.]+)[\s,;]+(Height=)?(-?[\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success) 
                    {
                        float x = float.Parse(match.Groups[2].Value, NumberStyles.Float, format);
                        float y = float.Parse(match.Groups[4].Value, NumberStyles.Float, format);
                        float w = float.Parse(match.Groups[6].Value, NumberStyles.Float, format);
                        float h = float.Parse(match.Groups[8].Value, NumberStyles.Float, format);
                        return new RectangleF(x, y, w, h);
                    }
                }
                if (_type == typeof(Margins))
                {
                    Match match = Regex.Match(_value, @"(Top=)?(-?[\d\.]+)[\s,;]+(Left=)?(-?[\d\.]+)[\s,;]+(Bottom=)?(-?[\d\.]+)[\s,;]+(Right=)?(-?[\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success)
                    {
                        float t = float.Parse(match.Groups[2].Value, NumberStyles.Float, format);
                        float l = float.Parse(match.Groups[4].Value, NumberStyles.Float, format);
                        float b = float.Parse(match.Groups[6].Value, NumberStyles.Float, format);
                        float r = float.Parse(match.Groups[8].Value, NumberStyles.Float, format);
                        return new Margins(t, l, b, r );
                    }
                }
                if (_type == typeof(PointF))
                {
                    Match match = Regex.Match(_value, @"(X=)?(-?[\d\.]+)[\s,;]+(Y=)?(-?[\d\.]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                    if (match.Success)
                    {
                        float x = float.Parse(match.Groups[2].Value, NumberStyles.Float, format);
                        float y = float.Parse(match.Groups[4].Value, NumberStyles.Float, format);
                        return new PointF(x, y);
                    }
                }
                if (_type == typeof(float))
                {
                    return float.Parse(_value, format);
                }
                if (_type == typeof(double))
                {
                    return double.Parse(_value, format);
                }
                if (_type == typeof(Rectangle))
                {
                    Match match = Regex.Match(_value, @"(X=)?(-?\d+)[\s,;]+(Y=)?(-?\d+)[\s,;]+(Width=)?(-?\d+)[\s,;]+(Height=)?(-?\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
                if (_type == typeof(float[]))
                {
                    MatchCollection matches = Regex.Matches(_value, @"(-?[\d\.]+)([\,\;\s]+|$)");
                    List<float> values = new List<float>(matches.Count);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            values.Add(float.Parse(match.Groups[1].Value, NumberStyles.Float, format));
                        }
                    }
                    return values.ToArray();
                }
            }

            return null;
        }
    }
}
