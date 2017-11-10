using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Utility
{
    public class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerable<Task<IList<T>>> _batchLoader;

        internal AsyncEnumerator(IEnumerable<Task<IList<T>>> batchLoader)
        {
            _batchLoader = batchLoader ?? throw new ArgumentNullException(nameof(batchLoader));
        }

        public IEnumerable<Task<IList<T>>> Items => _batchLoader;

        public IAsyncEnumerator<T2> SplitEachItem<T2>(Func<T, IEnumerable<T2>> transformer)
        {
            var tx = _batchLoader.Select(t =>
            {
               return t.ContinueWith<IList<T2>>(b =>
               {
                   return b.Result.SelectMany(transformer).ToList();
               });
            });

            return new AsyncEnumerator<T2>(tx);
        }

        public IAsyncEnumerator<T2> TransformEachItem<T2>(Func<T, T2> transformer)
        {
            var tx = _batchLoader.Select(t =>
            {
                return t.ContinueWith<IList<T2>>(b =>
                {
                    return b.Result.Select(transformer).ToList();
                });
            });

            return new AsyncEnumerator<T2>(tx);
        }

        public IAsyncEnumerator<T2> TransformEachBatch<T2>(Func<IList<T>, IList<T2>> transformer)
        {
            var tx = _batchLoader.Select(t =>
            {
                return t.ContinueWith(b => transformer(b.Result));
            });

            return new AsyncEnumerator<T2>(tx);
        }

        public async Task<bool> ProcessUsing(Func<AsyncBatch<T>, bool> processor)
        {
            int i = 0;

            foreach (var batchTask in _batchLoader)
            {
                var items = await batchTask;

                if (!processor(new AsyncBatch<T>(items, i++))) return false;
            }

            return true;
        }
    }
}