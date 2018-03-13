using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    internal class AsyncPipe<T> : IAsyncPipe<T>
    {
        private readonly List<IAsyncSink<T>> _sinks;

        public AsyncPipe(IAsyncSource<T> source)
        {
            _sinks = new List<IAsyncSink<T>>();
            Source = source;
        }

        public IAsyncSource<T> Source { get; }

        public IEnumerable<IAsyncSink<T>> Sinks => _sinks;

        public event EventHandler Disposing;

        public IAsyncPipe<T> RegisterSinks(params IAsyncSink<T>[] sinks)
        {
            _sinks.AddRange(sinks);
            return this;
        }

        public async Task RunAsync(CancellationToken cancellationToken, int epochs = 1)
        {
            ArgAssert.AssertGreaterThanZero(epochs, nameof(epochs));

            var pipelineInstance = Guid.NewGuid().ToString("N");

            for (var i = 0; i < epochs; i++)
            {
                if (!_sinks.Any(s => s.CanReceive))
                {
                    return;
                }

                await Source.ProcessUsing(async b =>
                {
                    var activeSinks = _sinks.Where(s => s.CanReceive).ToList();

                    if (!activeSinks.Any())
                    {
                        return;
                    }

                    DebugOutput.Log($"{pipelineInstance} Processing batch {b} with {activeSinks.Count} active sinks");

                    var tasks = activeSinks.Select(s => s.ReceiveAsync(b, cancellationToken)).ToList();

                    DebugOutput.Log($"Running {tasks.Count} tasks");

                    await Task.WhenAll(tasks);

                }, cancellationToken);
            }
        }

        public void Dispose()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}