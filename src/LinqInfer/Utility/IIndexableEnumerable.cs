using System.Collections.Generic;

namespace LinqInfer.Utility
{
    public interface IIndexableEnumerable<T> : IEnumerable<T>
    {
        int Count { get; }
        T this[int index] { get; }
    }
}