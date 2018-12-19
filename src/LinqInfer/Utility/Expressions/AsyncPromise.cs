using System;
using System.Threading.Tasks;

namespace LinqInfer.Utility.Expressions
{
    class AsyncPromise<T> : IAsyncPromise<T>
    {
        readonly Lazy<Task<T>> _task;

        AsyncPromise(Func<Task<T>> taskFactory)
        {
            _task = new Lazy<Task<T>>(taskFactory);
        }

        public static IAsyncPromise<T> Create(Task<T> task)
        {
            return new AsyncPromise<T>(() => task);
        }

        public Exception Error => _task.Value.Exception;

        public T Result => _task.Value.ConfigureAwait(false).GetAwaiter().GetResult();

        public IPromise<TOutput> Then<TOutput>(Func<T, TOutput> work)
        {
            return new AsyncPromise<TOutput>(async () => work(await _task.Value));
        }

        public IPromise<TOutput> ThenAsync<TOutput>(Func<T, Task<TOutput>> work)
        {
            return new AsyncPromise<TOutput>(async () => await work(await _task.Value));
        }
    }
}