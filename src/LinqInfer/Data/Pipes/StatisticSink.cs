using LinqInfer.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    sealed class StatisticSink<T> : IAsyncSink<T>, IPipeStatistics
    {
        readonly IStopwatch _sw;
        long _batchesReceived;
        long _itemsReceived;

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
            DebugOutput.Log($"Stats Batch {dataBatch.BatchNumber}");

            if (!_sw.IsRunning)
            {
                DebugOutput.Log("Starting stats");
                _sw.Start();
            }

            Interlocked.Increment(ref _batchesReceived);
            Interlocked.Add(ref _itemsReceived, dataBatch.Items.Count);

            if (dataBatch.IsLast)
            {
                DebugOutput.Log("Stoping stats");
                _sw.Stop();
            }

            return Task.FromResult(0);
        }
    }
}