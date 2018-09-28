using System;

namespace LinqInfer.Utility.Expressions
{
    public sealed class Promise<T> : IPromise<T>
    {
        readonly Lazy<T> _result;

        public Promise(Func<T> resultFactory)
        {
            _result = new Lazy<T>(() =>
            {
                try
                {
                    return resultFactory();
                }
                catch (Exception ex)
                {
                    Error = ex;
                    throw;
                }
            }, true);
        }

        public Promise<TOutput> Then<TOutput>(Func<T, TOutput> work)
        {
            return new Promise<TOutput>(() => work(Result));
        }

        public T Result => _result.Value;

        public Exception Error { get; private set; }
    }
}