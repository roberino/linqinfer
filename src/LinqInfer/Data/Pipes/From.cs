using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Pipes
{
    /// <summary>
    /// Creates async enumerators from various sources
    /// </summary>
    public static class From
    {
        /// <summary>
        /// Converts an enumeration into an async enumerator
        /// </summary>
        public static IAsyncEnumerator<T> Enumerable<T>(IEnumerable<T> values, int batchSize = 1000)
        {
            return new AsyncEnumerator<T>(values
                .AsQueryable()
                .Chunk(batchSize)
                .Select(b => Task.FromResult<IList<T>>(b.ToList())));
        }

        /// <summary>
        /// Converts an enumeration into an async enumerator
        /// </summary>
        public static IAsyncEnumerator<T> Query<T>(IQueryable<T> values, long? estimatedResultCount = null, int batchSize = 1000)
        {
            return new AsyncEnumerator<T>(values
                .Chunk(batchSize)
                .Select(b => Task.FromResult<IList<T>>(b.ToList())), estimatedResultCount.HasValue ? estimatedResultCount.Value : values.Count());
        }

        /// <summary>
        /// Converts an enumeration of batch loading tasks
        /// into an async enumerable object
        /// </summary>
        /// <typeparam name="T">The type of each item in a batch of data</typeparam>
        /// <param name="batchLoader">An enumeration of tasks to load data</param>
        public static IAsyncEnumerator<T> EnumerableTasks<T>(IEnumerable<Task<IList<T>>> batchLoader)
        {
            return new AsyncEnumerator<T>(batchLoader);
        }

        /// <summary>
        /// Creates an async enumerator from a function
        /// </summary>
        /// <typeparam name="TInput">The type of data</typeparam>
        /// <param name="batchLoaderFunc">A batch loading function</param>
        public static IAsyncEnumerator<TInput> Func<TInput>(Func<int, AsyncBatch<TInput>> batchLoaderFunc)
        {
            var asyncEnum = new AsyncEnumerable<TInput>(batchLoaderFunc);

            var asyncEnumerator = new AsyncEnumerator<TInput>(asyncEnum);

            return asyncEnumerator;
        }
    }
}