using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface ITransformingAsyncBatchSource<T> : IAsyncBatchSource<T>
    {
        bool SkipEmptyBatches { get; set; }
        Task<bool> ProcessUsing(Func<IBatch<T>, bool> processor);
        ITransformingAsyncBatchSource<T2> SplitEachItem<T2>(Func<T, IEnumerable<T2>> transformer);
        ITransformingAsyncBatchSource<T2> TransformEachBatch<T2>(Func<IList<T>, IList<T2>> transformer);
        ITransformingAsyncBatchSource<T2> TransformEachItem<T2>(Func<T, T2> transformer);
        ITransformingAsyncBatchSource<T> Filter(Func<T, bool> predicate);
        ITransformingAsyncBatchSource<T> Limit(long maxNumberOfItems);
    }
}