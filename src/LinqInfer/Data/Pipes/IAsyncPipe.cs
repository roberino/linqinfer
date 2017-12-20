﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    public interface IAsyncPipe<T>
    {
        IAsyncSource<T> Source { get; }

        IEnumerable<IAsyncSink<T>> Sinks { get; }

        IAsyncPipe<T> RegisterSinks(params IAsyncSink<T>[] sinks);

        Task RunAsync(CancellationToken cancellationToken);
    }
}