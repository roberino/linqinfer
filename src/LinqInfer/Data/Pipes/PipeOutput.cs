using System;

namespace LinqInfer.Data.Pipes
{
    public sealed class PipeOutput<T, O>
    {
        internal PipeOutput(IAsyncPipe<T> pipe, O output)
        {
            Pipe = pipe ?? throw new ArgumentNullException(nameof(pipe));
            Output = output;
        }

        public IAsyncPipe<T> Pipe { get; }

        public O Output { get; }
    }
}