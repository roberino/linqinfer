using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqInfer.Utility
{
    internal class DeferredEnumerator<T> : IEnumerable<T>
    {
        private readonly Func<IEnumerable<T>> _enumeratorFunc;

        public DeferredEnumerator(Func<IEnumerable<T>> enumeratorFunc)
        {
            _enumeratorFunc = enumeratorFunc;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _enumeratorFunc().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _enumeratorFunc().GetEnumerator();
        }
    }
}