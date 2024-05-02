using System;
using System.Collections.Generic;

namespace Smartproj.Utils
{
    public interface IKeyd<TKey>
    {
        TKey KeyId { get; }
        int Index { get; }
        int OrderBy { get; set; }
    }
    public interface IOwnered<T>
    {
        T Owner { get; set; }
    }
    public interface IDistance<T> : IEquatable<IDistance<T>>, IEqualityComparer<IDistance<T>> where T : IComparable<T>
    {
        T DistanceTo(IDistance<T> _other);
        T[] Points { get; }
        int Rank { get; }
        T Avarage(IEnumerable<T> _array);
        T Sigma(IEnumerable<T> _array);
        T Sigma(IEnumerable<T> _array, T _centroid);
        T MaxValue { get; }
        T MinValue { get; }
        IDistance<T> Create(T[] _points);
        int Index { get; }
    }
}
