using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    sealed class AsyncBatchEnumerable<T> : IEnumerable<Task<IList<T>>>
    {
        readonly Func<int, AsyncBatch<T>> _loader;

        public AsyncBatchEnumerable(Func<int, AsyncBatch<T>> loader)
        {
            _loader = loader;
        }

        public IEnumerator<Task<IList<T>>> GetEnumerator()
        {
            int i = 0;

            AsyncBatch<T> next = null;

            while (!((next?.IsLast).GetValueOrDefault()))
            {
                next = _loader(i++);

                yield return next.ItemsLoader;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}