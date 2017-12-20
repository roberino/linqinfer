﻿using LinqInfer.Utility;
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

        public AsyncEnumerator(IEnumerable<Task<IList<T>>> batchLoader, long? estimatedTotalCount = null)
        {
            _batchLoader = batchLoader ?? throw new ArgumentNullException(nameof(batchLoader));
            _filters = new List<Func<T, bool>>();

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
            _filters.Add(predicate);
            return this;
        }

        private async Task<bool> ProcessUsing(Func<IBatch<T>, Task<bool>> processor)
        {
            ArgAssert.AssertNonNull(processor, nameof(processor));

            int i = 0;

            Batch<T> next = null;

            foreach (var batchTask in _batchLoader)
            {
                if (next != null)
                {
                    if(!(await processor(next))) return false;
                }

                var items = Filter(await batchTask);

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