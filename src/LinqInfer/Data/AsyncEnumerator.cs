using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerable<Task<IList<T>>> _batchLoader;

        public AsyncEnumerator(IEnumerable<Task<IList<T>>> batchLoader)
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

        public Task<bool> ProcessUsing(Func<IBatch<T>, bool> processor)
        {
            return ProcessUsing(b => Task.FromResult(processor(b)));
        }

        public async Task<bool> ProcessUsing(Func<IBatch<T>, Task<bool>> processor)
        {
            int i = 0;

            Batch<T> next = null;

            foreach (var batchTask in _batchLoader)
            {
                if (next != null)
                {
                    if (!(await processor(next))) return false;
                }

                var items = await batchTask;

                next = new Batch<T>(items, i++);
            }

            if (next != null)
            {
                next.IsLast = true;
                if (!(await processor(next))) return false;
            }

            return true;
        }
    }
}