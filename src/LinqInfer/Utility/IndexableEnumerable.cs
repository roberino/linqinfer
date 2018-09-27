using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Utility
{
    sealed class IndexableEnumerable<T> : IIndexedEnumerable<T>
    {
        readonly IEnumerable<T> _values;
        readonly Func<int, T> _indexFunction;
        readonly Func<int> _countFunction;

        public IndexableEnumerable(IEnumerable<T> values, Func<int, T> indexFunction = null)
        {
            _values = values;
            _indexFunction = indexFunction ?? (x => _values.ElementAt(x));
            _countFunction = _values.Count;
        }

        public IndexableEnumerable(IList<T> values)
        {
            _values = values;
            _indexFunction = x => ((IList<T>)_values)[x];
            _countFunction = () => ((IList<T>)_values).Count;
        }

        public IEnumerable<T> InnerEnumerable => _values;

        public T this[int index] => _indexFunction(index);

        public int Count => _countFunction();

        public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}