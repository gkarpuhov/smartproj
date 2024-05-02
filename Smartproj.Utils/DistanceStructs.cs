using System;
using System.Collections.Generic;

namespace Smartproj.Utils
{
    public struct PointD : IDistance<double>
    {
        private readonly double[] mPoints;
        public double X => mPoints[0];
        public double Y => mPoints[1];
        double[] IDistance<double>.Points => mPoints;
        double IDistance<double>.MaxValue => double.MaxValue;
        double IDistance<double>.MinValue => double.MinValue;
        public int Rank => mPoints.Length;
        public int Index { get; }
        public PointD(double _x, double _y, int _index) : this(_index, new double[2] { _x, _y }) { }
        public PointD(int _index, params double[] _points)
        {
            mPoints = _points;
            Index = _index;
        }
        public PointD(params double[] _points) : this(-1, _points) { }
        // Метод для вычисления Евклидова расстояния между двумя точками
        public double DistanceTo(IDistance<double> _other)
        {
            double sum = 0;
            for (int i = 0; i < mPoints.Length; i++)
            {
                sum += (mPoints[i] - _other.Points[i]) * (mPoints[i] - _other.Points[i]);
            }
            return Math.Sqrt(sum);
        }
        // IDistance
        double IDistance<double>.DistanceTo(IDistance<double> _other) => DistanceTo(_other);
        double IDistance<double>.Avarage(IEnumerable<double> _array) => _array.Avg();
        double IDistance<double>.Sigma(IEnumerable<double> _array) => _array.S1();
        double IDistance<double>.Sigma(IEnumerable<double> _array, double _centroid) => _array.S1(_centroid);
        IDistance<double> IDistance<double>.Create(double[] _points) => new PointD(_points);
        // IEquatable
        bool IEquatable<IDistance<double>>.Equals(IDistance<double> _other) => Equals(_other);
        public bool Equals(IDistance<double> _other)
        {
            if (mPoints.Length != _other.Points.Length) return false;

            for (int i = 0; i < mPoints.Length; i++)
            {
                if (!mPoints[i].Equals(_other.Points[i])) return false;
            }
            return true;
        }
        // IEqualityComparer
        bool IEqualityComparer<IDistance<double>>.Equals(IDistance<double> _x, IDistance<double> _y) => Equals(_x, _y);
        int IEqualityComparer<IDistance<double>>.GetHashCode(IDistance<double> _obj) => GetHashCode(_obj);
        public bool Equals(IDistance<double> _x, IDistance<double> _y) => _x.Index.Equals(_y.Index);
        public int GetHashCode(IDistance<double> _obj) => _obj.Index.GetHashCode();
        //
    }
    public struct PointI : IDistance<int>
    {
        private readonly int[] mPoints;
        public int X => mPoints[0];
        public int Y => mPoints[1];
        int[] IDistance<int>.Points => mPoints;
        int IDistance<int>.MaxValue => int.MaxValue;
        int IDistance<int>.MinValue => int.MinValue;
        public int Rank => mPoints.Length;
        public int Index { get; }
        public PointI(int _x, int _y, int _index) : this(_index, new int[2] { _x, _y }) { }
        public PointI(int _index, params int[] _points)
        {
            mPoints = _points;
            Index = _index;
        }
        public PointI(params int[] _points) : this(-1, _points) { }
        // Метод для вычисления Евклидова расстояния между двумя точками
        public int DistanceTo(IDistance<int> _other)
        {
            long sum = 0;
            for (int i = 0; i < mPoints.Length; i++)
            {
                sum += (mPoints[i] - _other.Points[i]) * (mPoints[i] - _other.Points[i]);
            }
            return (int)Math.Round(Math.Sqrt(sum));
        }
        // IDistance
        int IDistance<int>.DistanceTo(IDistance<int> _other) => DistanceTo(_other);
        int IDistance<int>.Avarage(IEnumerable<int> _array) => _array.Avg();
        int IDistance<int>.Sigma(IEnumerable<int> _array) => _array.S1();
        int IDistance<int>.Sigma(IEnumerable<int> _array, int _centroid) => _array.S1(_centroid);
        IDistance<int> IDistance<int>.Create(int[] _points) => new PointI(_points);
        // IEquatable
        bool IEquatable<IDistance<int>>.Equals(IDistance<int> _other) => Equals(_other);
        public bool Equals(IDistance<int> _other)
        {
            if (mPoints.Length != _other.Points.Length) return false;

            for (int i = 0; i < mPoints.Length; i++)
            {
                if (!mPoints[i].Equals(_other.Points[i])) return false;
            }
            return true;
        }
        // IEqualityComparer
        bool IEqualityComparer<IDistance<int>>.Equals(IDistance<int> _x, IDistance<int> _y) => Equals(_x, _y);
        int IEqualityComparer<IDistance<int>>.GetHashCode(IDistance<int> _obj) => GetHashCode(_obj);
        public bool Equals(IDistance<int> _x, IDistance<int> _y) => _x.Index.Equals(_y.Index);
        public int GetHashCode(IDistance<int> _obj) => _obj.Index.GetHashCode();
        //
    }
    public struct IntegerDistance : IDistance<int>
    {
        private readonly int[] mPoints;
        public int X => mPoints[0];
        int[] IDistance<int>.Points => mPoints;
        int IDistance<int>.MaxValue => int.MaxValue;
        int IDistance<int>.MinValue => int.MinValue;
        public int Rank => mPoints.Length;
        public int Index { get; }
        public IntegerDistance(int _point, int _index)
        {
            mPoints = new int[1] { _point };
            Index = _index;
        }
        public IntegerDistance(int _point) : this(_point, -1) {}

        public int DistanceTo(IDistance<int> _other)
        {
            return Math.Abs(mPoints[0] - _other.Points[0]);   
        }
        // IDistance
        int IDistance<int>.DistanceTo(IDistance<int> _other) => DistanceTo(_other);
        int IDistance<int>.Avarage(IEnumerable<int> _array) => _array.Avg();
        int IDistance<int>.Sigma(IEnumerable<int> _array) => _array.S1();
        int IDistance<int>.Sigma(IEnumerable<int> _array, int _centroid) => _array.S1(_centroid);
        IDistance<int> IDistance<int>.Create(int[] _points) => new IntegerDistance(_points[0]);
        // IEquatable
        bool IEquatable<IDistance<int>>.Equals(IDistance<int> _other) => Equals(_other);
        public bool Equals(IDistance<int> _other) => mPoints[0].Equals(_other.Points[0]);
        // IEqualityComparer
        bool IEqualityComparer<IDistance<int>>.Equals(IDistance<int> _x, IDistance<int> _y) => Equals(_x, _y);
        int IEqualityComparer<IDistance<int>>.GetHashCode(IDistance<int> _obj) => GetHashCode(_obj);
        public bool Equals(IDistance<int> _x, IDistance<int> _y) => _x.Index.Equals(_y.Index);
        public int GetHashCode(IDistance<int> _obj) => _obj.Index.GetHashCode();
        //
    }
}
