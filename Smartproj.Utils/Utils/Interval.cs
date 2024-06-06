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
}
