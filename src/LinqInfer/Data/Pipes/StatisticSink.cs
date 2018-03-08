using LinqInfer.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    internal sealed class StatisticSink<T> : IAsyncSink<T>, IPipeStatistics
    {
        private readonly IStopwatch _sw;
        private long _batchesReceived;
        private long _itemsReceived;

        public StatisticSink(IStopwatch stopwatch = null)
        {
            _sw = stopwatch ?? new StopwatchWrapper();
        }

        public bool CanReceive => true;

        public TimeSpan Elapsed => _sw.Elapsed;

        public long ItemsReceived => Interlocked.Read(ref _itemsReceived);

        public long BatchesReceived => Interlocked.Read(ref _batchesReceived);

        public TimeSpan AverageBatchReceiveTime => TimeSpan.FromMilliseconds(Elapsed.TotalMilliseconds / BatchesReceived);

        public double AverageBatchesPerSecond => BatchesReceived / Elapsed.TotalSeconds;

        public double AverageItemsPerSecond => ItemsReceived / Elapsed.TotalSeconds;

        public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
        {
            if (!_sw.IsRunning)
            {
                _sw.Start();
            }

            Interlocked.Increment(ref _batchesReceived);
            Interlocked.Add(ref _itemsReceived, dataBatch.Items.Count);

            if (dataBatch.IsLast)
            {
                _sw.Stop();
            }

            return Task.FromResult(0);
        }
    }
}