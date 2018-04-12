using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncEnumerator<T> : IAsyncSource<T>
    {
        bool SkipEmptyBatches { get; set; }
        IEnumerable<Task<IList<T>>> Items { get; }
        Task<bool> ProcessUsing(Func<IBatch<T>, bool> processor);
        IAsyncEnumerator<T2> SplitEachItem<T2>(Func<T, IEnumerable<T2>> transformer);
        IAsyncEnumerator<T2> TransformEachBatch<T2>(Func<IList<T>, IList<T2>> transformer);
        IAsyncEnumerator<T2> TransformEachItem<T2>(Func<T, T2> transformer);
        IAsyncEnumerator<T> Filter(Func<T, bool> predicate);
        IAsyncEnumerator<T> Limit(long maxNumberOfItems);
    }
}