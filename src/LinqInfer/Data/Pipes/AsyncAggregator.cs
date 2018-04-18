using LinqInfer.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    internal class AsyncAggregator<TInput, TAggregationKey, TAggregation> 
        : IBuilderSink<TInput, IDictionary<TAggregationKey, TAggregation>>
    {
        private readonly Func<TInput, KeyValuePair<TAggregationKey, TAggregation>> _selector;
        private readonly Func<TAggregation, TAggregation, TAggregation> _aggregator;
        private readonly ConcurrentDictionary<TAggregationKey, TAggregation> _results;

        public AsyncAggregator(Func<TInput, KeyValuePair<TAggregationKey, TAggregation>> selector, Func<TAggregation, TAggregation, TAggregation> aggregator)
        {
            ArgAssert.AssertNonNull(selector, nameof(selector));
            ArgAssert.AssertNonNull(aggregator, nameof(aggregator));

            _aggregator = aggregator;
            _selector = selector;

            _results = new ConcurrentDictionary<TAggregationKey, TAggregation>();
        }

        public bool CanReceive => true;

        public IDictionary<TAggregationKey, TAggregation> Output => _results;

        public Task ReceiveAsync(IBatch<TInput> dataBatch, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                foreach (var item in dataBatch.Items)
                {
                    var kv = _selector(item);

                    _results.AddOrUpdate(kv.Key, k => kv.Value, (k, v) =>
                    {
                        return _aggregator(v, kv.Value);
                    });
                }
            });
        }
    }
}