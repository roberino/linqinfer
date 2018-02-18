using System;

namespace LinqInfer.Utility
{
    public sealed class Factory<TResult, TArgs> : IFactory<TResult, TArgs>
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

        public static implicit operator Factory<TResult, TArgs>(Func<TArgs, TResult> factoryFunc)
        {
            return new Factory<TResult, TArgs>(factoryFunc);
        }
    }
}