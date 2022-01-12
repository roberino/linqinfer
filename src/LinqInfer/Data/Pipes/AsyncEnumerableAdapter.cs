using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    class AsyncEnumerableToBatchAdapter<T> : IEnumerable<Task<IList<T>>>
    {
        readonly IAsyncEnumerable<T> _data;
        readonly int _batchSize;

        public AsyncEnumerableToBatchAdapter(IAsyncEnumerable<T> data, int batchSize)
        {
            _data = data;
            _batchSize = batchSize;
        }

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();

        public IEnumerator<Task<IList<T>>> GetEnumerator() 
            => new AsyncEnumeratorAdapter(() => _data.GetAsyncEnumerator(), _batchSize);

        class AsyncEnumeratorAdapter : IEnumerator<Task<IList<T>>>
        {
            bool _advance;
            bool _complete;
            IAsyncEnumerator<T> _innerEnumerator;

            readonly List<T> _current;
            readonly Func<IAsyncEnumerator<T>> _innerEnumeratorFactory;
            readonly int _batchSize;

            public AsyncEnumeratorAdapter(Func<IAsyncEnumerator<T>> innerEnumeratorFactory, int batchSize)
            {
                _innerEnumeratorFactory = innerEnumeratorFactory;
                _innerEnumerator = innerEnumeratorFactory();                
                _batchSize = batchSize;
                _current = new(_batchSize);
            }

            public Task<IList<T>> Current
            {
                get
                {
                    if (_advance)
                        return LoadNext();

                    return Task.FromResult<IList<T>>(_current);
                }
            }

            async Task<IList<T>> LoadNext()
            {
                if (!_advance || _complete)
                    return _current;

                _advance = false;
                _current.Clear();

                for (var i = 0; i < _batchSize; i++)
                {
                    if (!(await _innerEnumerator.MoveNextAsync()))
                    {
                        _complete = true;
                        
                        await _innerEnumerator.DisposeAsync();

                        break;
                    }

                    _current.Add(_innerEnumerator.Current);
                }

                return _current;
            }

            object IEnumerator.Current { get; }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_complete)
                    return false;

                _advance = true;
                return true;
            }

            public void Reset()
            {
                _complete = false;
                _advance = false;
                _innerEnumerator = _innerEnumeratorFactory();
            }
        }
    }
}
