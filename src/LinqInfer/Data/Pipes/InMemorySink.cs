using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public class InMemorySink<T> : ConcurrentQueue<T>, IAsyncSink<T>
    {
        public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
        {
            foreach(var item in dataBatch.Items)
            {
                Enqueue(item);
            }

            return Task.FromResult(0);
        }
    }
}