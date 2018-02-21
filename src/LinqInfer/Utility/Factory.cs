using System;

namespace LinqInfer.Utility
{
    public sealed class Factory<TArgs, TResult> : IFactory<TResult, TArgs>
    {
        private readonly Func<TArgs, TResult> _factoryFunc;

        public Factory(Func<TArgs, TResult> factoryFunc)
        {
            _factoryFunc = ArgAssert.AssertNonNull(factoryFunc, nameof(factoryFunc));
        }

        public TResult Create(TArgs parameters)
        {
            return _factoryFunc(parameters);
        }

        public static implicit operator Factory<TArgs, TResult>(Func<TArgs, TResult> factoryFunc)
        {
            return new Factory<TArgs, TResult>(factoryFunc);
        }
    }
}