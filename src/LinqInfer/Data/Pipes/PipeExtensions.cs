using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data.Pipes
{
    public static class PipeExtensions
    {
        public static IAsyncPipe<T> CreatePipe<T>(this IAsyncEnumerator<T> asyncEnumerator)
        {
            return new AsyncPipe<T>(asyncEnumerator);
        }

        public static PipeOutput<TInput, IDictionary<TAggregationKey, TAggregation>> AttachAggregator<TInput, TAggregationKey, TAggregation>(this IAsyncPipe<TInput> pipe, Func<TInput, KeyValuePair<TAggregationKey, TAggregation>> keySelector, Func<TAggregation, TAggregation, TAggregation> aggregator)
        {
            var asyncAgg = new AsyncAggregator<TInput, TAggregationKey, TAggregation>(keySelector, aggregator);

            return pipe.Attach(asyncAgg);
        }

        public static PipeOutput<T, O> Attach<T, O> (this IAsyncPipe<T> pipe, IBuilderSink<T, O> builder)
        {
            pipe.RegisterSinks(builder);

            return new PipeOutput<T, O>(pipe, builder.Output);
        }

        public static PipeOutput<T, IPipeStatistics> TrackStatistics<T>(this IAsyncPipe<T> pipe)
        {
            var stats = new StatisticSink<T>();

            pipe.RegisterSinks(stats);

            return new PipeOutput<T, IPipeStatistics>(pipe, stats);
        }

        public static IEnumerable<S> GetSinks<T, S>(this IAsyncPipe<T> pipe) where S : IAsyncSink<T>
        {
            return pipe.Sinks.Where(s => s is S).Cast<S>();
        }

        public static S GetSink<T, S>(this IAsyncPipe<T> pipe) where S : IAsyncSink<T>
        {
            return (S)pipe.Sinks.Where(s => s is S).FirstOrDefault();
        }
    }
}
