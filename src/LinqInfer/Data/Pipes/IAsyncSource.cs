using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncSource<T>
    {
        Task<bool> ProcessUsing(Action<IBatch<T>> processor, CancellationToken cancellationToken);
        Task<bool> ProcessUsing(Func<IBatch<T>, Task> processor, CancellationToken cancellationToken);
    }
}