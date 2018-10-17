using System;
using System.Threading.Tasks;

namespace LinqInfer.Utility.Expressions
{
    public interface IAsyncPromise<out T> : IPromise<T>
    {
        IPromise<TOutput> ThenAsync<TOutput>(Func<T, Task<TOutput>> work);
    }
}