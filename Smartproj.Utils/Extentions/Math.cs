using System;
using System.Collections.Generic;
using System.Linq;

namespace Smartproj.Utils
{
    public static class MathEx
    {
        public static bool IsOdd(this int _value)
        {
            return _value % 2 == 0;
        }
        public static bool KMeans<T>(this IEnumerable<IDistance<T>> _points, T _sigma, out List<KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>> _result, int _maxIterations = 100) where T : IComparable<T>
        {
            _result = null;
            if (_points.Count() > 1)
            {
                for (int iteration = 0; iteration < _maxIterations; iteration++)
                {
                    _result = KMeans(_points, iteration + 1, _maxIterations);
                    bool theend = true;
                    for (int i = 0; theend && i < _result.Count; i++)
                    {
                        for (int j = 0; theend && j < _result[i].Key.Points.Length; j++)
                        {
                            if (_result[i].Value.Count > 0)
                            {
                                if (_result[i].Key.Sigma(_result[i].Value.Select(x => x.Points[j]), _result[i].Key.Points[j]).CompareTo(_sigma) > 0) theend = false;
                            }
                        }
                    }

                    if (theend) return true;
                }
            }
            return false;
        }

        // Метод KMeans для кластеризации точек
        public static List<KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>> KMeans<T>(IEnumerable<IDistance<T>> _points, int _k, int _maxIterations = 100) where T : IComparable<T>
        {
            Random random = new Random();
            // Инициализируем центроиды случайными точками из набора данных
            List<KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>> centroids = _points.OrderBy(p => random.Next()).Take(_k).ToList().ConvertAll(x => new KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>(x, new HashSet<IDistance<T>>()));
            // Выполняем итерации до сходимости или достижения максимального количества итераций
            for (int iteration = 0; iteration < _maxIterations; iteration++)
            {
                // Инициализируем кластеры
                for (int i = 0; i < _k; i++)
                {
                    centroids[i].Value.Clear();
                }

                // Для каждой точки находим ближайший центроид и добавляем точку в соответствующий кластер
                foreach (var point in _points)
                {

                    int closestCentroidIndex = 0;
                    T minDistance = point.MaxValue;

                    for (int i = 0; i < centroids.Count; i++)
                    {
                        T distance = point.DistanceTo(centroids[i].Key);

                        if (distance.CompareTo(minDistance) < 0)
                        {
                            minDistance = distance;
                            closestCentroidIndex = i;
                        }
                    }
                    centroids[closestCentroidIndex].Value.Add(point);
                }

                // Обновляем центроиды
                bool centroidsChanged = false;
                for (int i = 0; i < _k; i++)
                {
                    if (centroids[i].Value.Count > 0)
                    {
                        T[] pointsAvg = new T[centroids[i].Key.Points.Length];
                        for (int j = 0; j < pointsAvg.Length; j++)
                        {
                            pointsAvg[j] = centroids[i].Key.Avarage(centroids[i].Value.Select(x => x.Points[j]));
                        }

                        IDistance<T> newCentroid = centroids[i].Key.Create(pointsAvg);

                        if (!newCentroid.Equals(centroids[i].Key))
                        {
                            centroidsChanged = true;
                            centroids[i] = new KeyValuePair<IDistance<T>, ICollection<IDistance<T>>>(newCentroid, centroids[i].Value);
                        }
                    }
                }

                // Если центроиды не изменились, завершаем алгоритм
                if (!centroidsChanged)
                {
                    break;
                }
            }
            return centroids;
        }
        public static double Avg(this double[] _items)
        {
            int count = _items.Length;
            if (count == 0)
            {
                throw new ArgumentException("Avg; Возможно деление на 0");
            }
            double sum = 0;
            for (int i = 0; i < count; i++) { sum = sum + _items[i]; }
            return sum / count;
        }
        public static double S1(this double[] _items) => _items.S1(Avg(_items));
        public static double S1(this double[] _items, double _centroid)
        {
            int count = _items.Length;
            if (count > 1)
            {
                double sum = 0;
                for (int i = 0; i < count; i++)
                {
                    sum = sum + (_items[i] - _centroid) * (_items[i] - _centroid);
                }

                return Math.Sqrt(sum / count);
            }
            else
                return 0;
        }
        public static double Avg(this IEnumerable<double> _items)
        {
            int count = _items.Count();
            if (count == 0)
            {
                throw new ArgumentException("Avg; Возможно деление на 0");
            }
            double sum = 0;
            foreach (double item in _items) { sum = sum + item; }

            return sum / count;
        }
        public static double S1(this IEnumerable<double> _items) => _items.S1(Avg(_items));
        public static double S1(this IEnumerable<double> _items, double _centroid)
        {
            int count = _items.Count();
            if (count > 1)
            {
                double sum = 0;
                foreach (double item in _items)
                {
                    sum = sum + (item - _centroid) * (item - _centroid);
                }

                return Math.Sqrt(sum / count);
            }
            else
                return 0;
        }
        public static double S2(this IEnumerable<double> _items) { return _items.S2(_items.Avg()); }
        public static double S2(this IEnumerable<double> _items, double _centroid)
        {
            int count = _items.Count();
            if (count > 2)
            {
                double sum = 0;
                foreach (double item in _items)
                {
                    sum = sum + (item - _centroid) * (item - _centroid);
                }

                return Math.Sqrt(sum / (count - 1));
            }
            else
                return 0;
        }
        public static int Avg(this int[] _items)
        {
            long count = _items.Length;
            if (count == 0)
            {
                throw new ArgumentException("Avg; Возможно деление на 0");
            }
            long sum = 0;
            for (int i = 0; i < count; i++) { sum = sum + (long)_items[i]; }
            return (int)(sum / count);
        }
        public static int S1(this int[] _items) => _items.S1(Avg(_items));
        public static int S1(this int[] _items, int _centroid)
        {
            if (_items.Length > 1)
            {
                long sum = 0;
                for (int i = 0; i < _items.Length; i++)
                {
                    sum = sum + ((long)_items[i] - (long)_centroid) * ((long)_items[i] - (long)_centroid);
                }
                return (int)Math.Sqrt(sum / _items.Length);
            }
            else
                return 0;
        }
        public static int Avg(this IEnumerable<int> _items)
        {
            long count = _items.Count();
            if (count == 0)
            {
                throw new ArgumentException("Avg; Возможно деление на 0");
            }
            long sum = 0;
            foreach (long item in _items) { sum = sum + item; }

            return (int)(sum / count);
        }
        public static int S1(this IEnumerable<int> _items) => _items.S1(Avg(_items));
        public static int S1(this IEnumerable<int> _items, int _centroid)
        {
            long count = _items.Count();
            if (count > 1)
            {
                long sum = 0;
                foreach (long item in _items)
                {
                    sum = sum + (item - (long)_centroid) * (item - (long)_centroid);
                }

                return (int)Math.Sqrt(sum / count);
            }
            else
                return 0;
        }
        public static int S2(this int[] _items) => _items.S2(Avg(_items));
        public static int S2(this int[] _items, int _centroid)
        {
            if (_items.Length > 2)
            {
                long sum = 0;
                for (int i = 0; i < _items.Length; i++)
                {
                    sum = sum + ((long)_items[i] - (long)_centroid) * ((long)_items[i] - (long)_centroid);
                }
                return (int)Math.Sqrt(sum / (_items.Length - 1));
            }
            else
                return 0;
        }
        public static int S2(this IEnumerable<int> _items) => _items.S2(Avg(_items));
        public static int S2(this IEnumerable<int> _items, int _centroid)
        {
            long count = _items.Count();
            if (count > 2)
            {
                long sum = 0;
                foreach (long item in _items)
                {
                    sum = sum + (item - (long)_centroid) * (item - (long)_centroid);
                }

                return (int)Math.Sqrt(sum / (count - 1));
            }
            else
                return 0;
        }
    }
}
