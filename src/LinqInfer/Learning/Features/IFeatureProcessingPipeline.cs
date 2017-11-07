using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Represents a pipeline of feature data
    /// which can be transformed and processed
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    public interface IFeatureProcessingPipeline<T> : IFeatureDataSource, IFeatureTransformBuilder<T> where T : class
    {
        /// <summary>
        /// Returns an enumeration of vector data in batches.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IList<ObjectVector<T>>> ExtractBatches(int batchSize = 1000);

        /// <summary>
        /// Processes the data using the supplied function
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="processor">A processing function which takes a pipeline and an output name</param>
        ExecutionPipline<TResult> ProcessWith<TResult>(Func<IFeatureProcessingPipeline<T>, string, TResult> processor);

        /// <summary>
        /// Processes the data using the supplied asyncronous function
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="processor">A processing function which takes a pipeline and an output name</param>
        /// <returns></returns>
        ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<IFeatureProcessingPipeline<T>, string, Task<TResult>> processor);
    }
}