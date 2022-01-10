using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqInfer.Text.Tokenisers
{
    class DisposableEnumerator<T> : IEnumerable<T>
    {
        readonly IEnumerable<T> _innerEnumerator;
        readonly IDisposable _disposable;

        public DisposableEnumerator(IEnumerable<T> innerEnumerator, IDisposable disposable)
        {
            _innerEnumerator = innerEnumerator;
            _disposable = disposable;
        }

        public IEnumerator<T> GetEnumerator() 
            => new EnumeratorImpl(_innerEnumerator.GetEnumerator(), _disposable);

        IEnumerator IEnumerable.GetEnumerator()
            => new EnumeratorImpl(_innerEnumerator.GetEnumerator(), _disposable);

        class EnumeratorImpl : IEnumerator<T>
        {
            readonly IEnumerator<T> _innerEnumerator;
            readonly IDisposable _disposable;

            public EnumeratorImpl(IEnumerator<T> innerEnumerator, IDisposable disposable)
            {
                _innerEnumerator = innerEnumerator;
                _disposable = disposable;
            }

            public T Current => _innerEnumerator.Current;
            object IEnumerator.Current => _innerEnumerator.Current;

            public void Dispose()
            {
                _innerEnumerator.Dispose();
                _disposable.Dispose();
            }

            public bool MoveNext() => _innerEnumerator.MoveNext();

            public void Reset() => _innerEnumerator.Reset();
        }
    }
}