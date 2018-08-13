using LinqInfer.Utility;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    class AsyncPipe<T> : AsyncPipeBase<T>
    {
        public AsyncPipe(IAsyncSource<T> source)
        {
            Source = source;
        }

        public override IAsyncSource<T> Source { get; }

        public override async Task RunAsync(CancellationToken cancellationToken, int epochs = 1)
        {
            ArgAssert.AssertGreaterThanZero(epochs, nameof(epochs));

            var pipelineInstance = Guid.NewGuid().ToString("N");

            for (var i = 0; i < epochs; i++)
            {
                if (!Sinks.Any(s => s.CanReceive))
                {
                    return;
                }

                await Source.ProcessUsing(async b =>
                {
                    var activeSinks = Sinks.Where(s => s.CanReceive).ToList();

                    if (!activeSinks.Any())
                    {
                        return;
                    }

                    DebugOutput.Log($"{pipelineInstance} Processing epoch {i}, batch {b} with {activeSinks.Count} active sink(s)");

                    var tasks = activeSinks.Select(s => s.ReceiveAsync(b, cancellationToken)).ToList();

                    DebugOutput.Log($"Running {tasks.Count} tasks");

                    await Task.WhenAll(tasks);

                }, cancellationToken);
            }
        }
    }
}