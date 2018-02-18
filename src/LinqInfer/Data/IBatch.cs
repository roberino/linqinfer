using System.Collections.Generic;

namespace LinqInfer.Data
{
    public interface IBatch<T>
    {
        IList<T> Items { get; }
        int BatchNumber { get; }
        bool IsLast { get; }
    }
}