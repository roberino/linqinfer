using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    internal class AsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerable<Task<IList<T>>> _batchLoader;
        private readonly IList<Func<T, bool>> _filters;
        private readonly long? _limit;

        public AsyncEnumerator(
            IEnumerable<Task<IList<T>>> batchLoader, 
            long? estimatedTotalCount = null,
            long? limit = null,
            IList<Func<T, bool>> filters = null)
        {
            _batchLoader = batchLoader ?? throw new ArgumentNullException(nameof(batchLoader));
            _filters = filters ?? new List<Func<T, bool>>();
            _limit = limit;

            EstimatedTotalCount = estimatedTotalCount;
        }

        public long? EstimatedTotalCount { get; }

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

        public Task<bool> ProcessUsing(Action<IBatch<T>> processor, CancellationToken cancellationToken)
        {
            ArgAssert.AssertNonNull(processor, nameof(processor));

            return ProcessUsing(b =>
            {
                if (cancellationToken.IsCancellationRequested) return false;

                processor(b);

                return true;
            });
        }

        public Task<bool> ProcessUsing(Func<IBatch<T>, bool> processor)
        {
            ArgAssert.AssertNonNull(processor, nameof(processor));

            return ProcessUsing(b => Task.FromResult(processor(b)));
        }

        public Task<bool> ProcessUsing(Func<IBatch<T>, Task> processor, CancellationToken cancellationToken)
        {
            ArgAssert.AssertNonNull(processor, nameof(processor));

            return ProcessUsing(async b =>
            {
                if (cancellationToken.IsCancellationRequested) return false;

                await processor(b);

                return true;
            });
        }

        public IAsyncEnumerator<T> Filter(Func<T, bool> predicate)
        {
            return new AsyncEnumerator<T>(_batchLoader, EstimatedTotalCount, _limit, _filters.Concat(new[] { predicate }).ToList());
        }

        public IAsyncEnumerator<T> Limit(long maxNumberOfItems)
        {
            return new AsyncEnumerator<T>(_batchLoader, EstimatedTotalCount, maxNumberOfItems, _filters);
        }

        private async Task<bool> ProcessUsing(Func<IBatch<T>, Task<bool>> processor)
        {
            ArgAssert.AssertNonNull(processor, nameof(processor));

            int i = 0;
            long counter = 0;

            Batch<T> next = null;

            foreach (var batchTask in _batchLoader)
            {
                if (next != null)
                {
                    if(!(await processor(next))) return false;
                }

                var items = Filter(await batchTask);

                if (_limit.HasValue)
                {
                    counter += items.Count;

                    if (counter >= _limit.Value)
                    {
                        var overflow = counter - _limit.Value;
                        var excess = (int)(next.Items.Count - overflow);

                        next = new Batch<T>(items.Take(excess).ToList(), i++);

                        break;
                    }
                }

                next = new Batch<T>(items, i++);
            }

            if (next != null)
            {
                next.IsLast = true;
                if (!(await processor(next))) return false;
            }

            return true;
        }

        private IList<T> Filter(IList<T> items)
        {
            if (_filters.Count == 0) return items;

            IEnumerable<T> filtered = items;

            foreach(var filter in _filters)
            {
                filtered = filtered.Where(filter);
            }

            return filtered.ToList();
        }
    }
}