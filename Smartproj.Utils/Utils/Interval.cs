using Microsoft.SqlServer.Server;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;

namespace Smartproj.Utils
{
    public struct Margins
    {
        public float Top;
        public float Left;
        public float Bottom;
        public float Right;
        public Margins(float _top, float _left, float _bottom, float _right)
        {
            Top = _top;
            Left = _left;
            Bottom = _bottom;
            Right = _right;
        }
        public override string ToString()
        {
            NumberFormatInfo format = new NumberFormatInfo() { NumberDecimalSeparator = "." };
            return $"{{Top={Top.ToString(format)},Left={Left.ToString(format)},Bottom={Bottom.ToString(format)},Right={Right.ToString(format)}}}";
        }
    }
    public class Interval<T> where T : IComparable<T>
    {
        public Interval(T _x1, T _x2, string _floatseparator)
        {
            mX1 = _x1;
            mX2 = _x2;
            mNumberDecimalSeparator = _floatseparator;
        }
        public Interval(T _x1, T _x2) : this(_x1, _x2, ".")
        {
        }
        public bool Contains(T _val)
        {
            return _val.CompareTo(mX1) >= 0 && _val.CompareTo(mX2) <= 0;
        }
        public bool TrySet(string _val)
        {
            string[] val = _val.Split('-');
            if (val.Length == 2 && val[0] != "" && val[1] != "")
            {
                if (typeof(T) == typeof(int))
                {
                    int val1 = 0;
                    int val2 = 0;
                    if (int.TryParse(val[0], out val1) && int.TryParse(val[1], out val2))
                    {
                        mX1 = (T)(object)val1;
                        mX2 = (T)(object)val2;
                        return true;
                    }
                    else
                        return false;
                }
                if (typeof(T) == typeof(byte))
                {
                    byte val1 = 0;
                    byte val2 = 0;
                    if (byte.TryParse(val[0], out val1) && byte.TryParse(val[1], out val2))
                    {
                        mX1 = (T)(object)val1;
                        mX2 = (T)(object)val2;
                        return true;
                    }
                    else
                        return false;
                }
                if (typeof(T) == typeof(float))
                {
                    float val1 = 0;
                    float val2 = 0;

                    NumberFormatInfo ni = new NumberFormatInfo();
                    ni.NumberDecimalSeparator = mNumberDecimalSeparator;

                    if (float.TryParse(val[0], NumberStyles.Float, ni, out val1) && float.TryParse(val[1], NumberStyles.Float, ni, out val2))
                    {
                        mX1 = (T)(object)val1;
                        mX2 = (T)(object)val2;
                        return true;
                    }
                    else
                        return false;
                }
                if (typeof(T) == typeof(double))
                {
                    double val1 = 0;
                    double val2 = 0;

                    NumberFormatInfo ni = new NumberFormatInfo();
                    ni.NumberDecimalSeparator = mNumberDecimalSeparator;

                    if (double.TryParse(val[0], NumberStyles.Float, ni, out val1) && double.TryParse(val[1], NumberStyles.Float, ni, out val2))
                    {
                        mX1 = (T)(object)val1;
                        mX2 = (T)(object)val2;
                        return true;
                    }
                    else
                        return false;
                }

                if (!this.Parse(_val))
                {
                    throw new ArgumentException("Недопустимый обобщенный тип Т");
                }
                else
                    return true;
            }
            else
            {
                return false;
            }
        }

        protected string mNumberDecimalSeparator;
        protected T mX1;
        public T X1 => mX1;
        protected T mX2;
        public T X2 => mX2;
        protected virtual bool Parse(string _val)
        {
            return false;
        }
        public static Interval<T> FromString(string _val)
        {
            Interval<T> ret = new Interval<T>(default(T), default(T));

            if (ret.TrySet(_val))
            {
                return ret;
            }
            else
                return null;
        }
    }

    public class Interval
    {
        public Interval(int _x1, int _x2)
        {
            X1 = _x1;
            X2 = _x2;
        }
        public Interval(ValueTuple<int, int> _interval) : this(_interval.Item1, _interval.Item2) { }
        public int X1 { get; set; }
        public int X2 { get; set; }
        public int Length => X2 - X1;
        public bool IsPoint => X1 == X2;
        public bool Contains(Interval _interval) => X2 >= _interval.X2 && X1 <= _interval.X1;
        public bool Contains(int _point) => X2 >= _point && X1 <= _point;
        public static bool Intersection(IEnumerable<Interval> _inetrvals, out Interval _intersection)
        {
            int x1 = int.MinValue;
            int x2 = int.MaxValue;
            _intersection = null;
            if (_inetrvals == null || _inetrvals.Count() == 0) return false;
            foreach (Interval interval in _inetrvals)
            {
                if (interval.X1 > x1) x1 = interval.X1;
                if (interval.X2 < x2) x2 = interval.X2;

                if (x1 > x2)
                {
                    return false;
                }
            }

            _intersection = new Interval(x1, x2);
            return true;
        }
    }
    public class IntervalF
    {
        public IntervalF(float _x1, float _x2)
        {
            X1 = _x1;
            X2 = _x2;
        }
        public IntervalF(ValueTuple<float, float> _interval) : this(_interval.Item1, _interval.Item2) { }
        public float X1 { get; set; }
        public float X2 { get; set; }
        public float Length => X2 - X1;
        public bool IsPoint => X1 == X2;
        public bool Contains(IntervalF _interval) => X2 >= _interval.X2 && X1 <= _interval.X1;
        public bool Contains(float _point) => X2 >= _point && X1 <= _point;
        public static bool Intersection(IEnumerable<IntervalF> _inetrvals, out IntervalF _intersection)
        {
            float x1 = float.MinValue;
            float x2 = float.MaxValue;
            _intersection = null;
            if (_inetrvals == null || _inetrvals.Count() == 0) return false;
            foreach (IntervalF interval in _inetrvals)
            {
                if (interval.X1 > x1) x1 = interval.X1;
                if (interval.X2 < x2) x2 = interval.X2;

                if (x1 > x2)
                {
                    return false;
                }
            }

            _intersection = new IntervalF(x1, x2);
            return true;
        }
    }

}
