using System;

namespace LinqInfer.Utility.Expressions
{
    public interface IPromise<out T>
    {
        Exception Error { get; }
        T Result { get; }

        IPromise<TOutput> Then<TOutput>(Func<T, TOutput> work);
    }
}