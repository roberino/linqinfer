using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    public interface IAsyncEnumerator<T>
    {
        IEnumerable<Task<IList<T>>> Items { get; }

        Task<bool> ProcessUsing(Func<IBatch<T>, bool> processor);
        Task<bool> ProcessUsing(Action<IBatch<T>> processor, CancellationToken cancellationToken);
        Task<bool> ProcessUsing(Func<IBatch<T>, Task> processor, CancellationToken cancellationToken);
        IAsyncEnumerator<T2> SplitEachItem<T2>(Func<T, IEnumerable<T2>> transformer);
        IAsyncEnumerator<T2> TransformEachBatch<T2>(Func<IList<T>, IList<T2>> transformer);
        IAsyncEnumerator<T2> TransformEachItem<T2>(Func<T, T2> transformer);
    }
}