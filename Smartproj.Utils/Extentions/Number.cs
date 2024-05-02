using System;

namespace Smartproj.Utils
{
    public static class FloatEx
    {
        public static int ToInt(this float _num) => (int)Math.Round(_num);
        public static float PointsToMM(this float _points) => _points * 0.3527777778f;
    }


    public static class IntegerEx
    {
        public static IDistance<int> ToDistance(this int _int, int _index = -1)
        {
            return new IntegerDistance(_int, _index);
        }
    }
    public static class EnumEx
    {
        public static bool HasFlag(this TagFileTypeEnum _this, TagFileTypeEnum _flag)
        {
            return (_this & _flag) == _flag;
        }
    }
}
