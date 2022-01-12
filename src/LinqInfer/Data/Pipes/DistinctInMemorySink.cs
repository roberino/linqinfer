using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    class DistinctInMemorySink<T> : IAsyncSink<T>
    {
        readonly HashSet<T> store;

        public DistinctInMemorySink(int? maxCapacity = null, IEqualityComparer<T> equalityComparer = null)
        {
            store = equalityComparer == null ? new HashSet<T>() : new HashSet<T>(equalityComparer);
        }

        public ISet<T> Data => store;

        public int? MaxCapacity { get; }

        public bool CanReceive => !MaxCapacity.HasValue || store.Count < MaxCapacity;

        public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
        {
            lock (store)
            {
                foreach (var item in dataBatch.Items)
                {
                    if (!CanReceive || cancellationToken.IsCancellationRequested) break;

                    if(!store.Contains(item))
                        store.Add(item);
                }
            }

            return Task.CompletedTask;
        }
    }
}