using System;

namespace LinqInfer.Data.Pipes
{
    public interface IPipeStatistics
    {
        TimeSpan AverageBatchReceiveTime { get; }
        long BatchesReceived { get; }
        TimeSpan Elapsed { get; }
        long ItemsReceived { get; }
        double AverageBatchesPerSecond { get; }
        double AverageItemsPerSecond { get; }
    }
}