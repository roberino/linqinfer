using System;

namespace LinqInfer.Utility.Expressions
{
    public sealed class InvocationResult<T>
    {
        internal InvocationResult(T result, Exception error = null)
        {
            Result = result;
            Error = error;
        }

        public T Result { get; }
        public bool Success => Error == null;
        public Exception Error { get; }
    }
}
