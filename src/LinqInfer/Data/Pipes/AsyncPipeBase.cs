using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    abstract class AsyncPipeBase<T> : IAsyncPipe<T>
    {
        readonly List<IAsyncSink<T>> _sinks;

        public AsyncPipeBase()
        {
            _sinks = new List<IAsyncSink<T>>();
        }

        public abstract IAsyncSource<T> Source { get; }

        public IEnumerable<IAsyncSink<T>> Sinks => _sinks;

        public event EventHandler Disposing;

        public IAsyncPipe<T> RegisterSinks(params IAsyncSink<T>[] sinks)
        {
            _sinks.AddRange(sinks);
            return this;
        }

        public abstract Task RunAsync(CancellationToken cancellationToken, int epochs = 1);

        public void Dispose()
        {
            Disposing?.Invoke(this, EventArgs.Empty);
        }
    }
}