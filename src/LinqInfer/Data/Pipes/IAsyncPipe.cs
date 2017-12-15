using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncPipe<T>
    {
        IAsyncSource<T> Source { get; }

        IAsyncPipe<T> RegisterSinks(params IAsyncSink<T>[] sinks);

        Task RunAsync(CancellationToken cancellationToken);
    }
}