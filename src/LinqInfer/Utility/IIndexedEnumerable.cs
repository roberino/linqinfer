using System.Collections.Generic;

namespace LinqInfer.Utility
{
    public interface IIndexedEnumerable<T> : IEnumerable<T>
    {
        int Count { get; }
        T this[int index] { get; }
    }
}