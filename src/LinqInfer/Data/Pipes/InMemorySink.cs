using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public class InMemorySink<T> : ConcurrentQueue<T>, IAsyncSink<T>
    {
        public InMemorySink(int? maxCapacity = null)
        {
            MaxCapacity = maxCapacity;
        }

        public int? MaxCapacity { get; }

        public bool CanReceive => !MaxCapacity.HasValue || Count < MaxCapacity;

        public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
        {
            foreach(var item in dataBatch.Items)
            {
                if (!CanReceive) break;

                Enqueue(item);
            }

            return Task.FromResult(0);
        }
    }
}