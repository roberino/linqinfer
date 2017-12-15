using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncSink<T>
    {
        Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken);
    }
}
