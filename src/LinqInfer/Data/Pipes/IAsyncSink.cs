using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncSink<T>
    {
        bool CanReceive { get; }

        Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken);
    }
}
