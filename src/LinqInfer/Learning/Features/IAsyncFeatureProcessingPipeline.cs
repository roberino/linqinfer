using LinqInfer.Utility;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Represents an asyncronous pipeline of feature data
    /// which can be transformed and processed
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    public interface IAsyncFeatureProcessingPipeline<T> where T : class
    {
        /// <summary>
        /// Returns an enumeration of vector data in batches.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<ObjectVector<T>> ExtractBatches();

        /// <summary>
        /// Processes the data using the supplied asyncronous function
        /// </summary>
        /// <typeparam name="TResult">The result type</typeparam>
        /// <param name="processor">A processing function which takes a pipeline and an output name</param>
        /// <returns></returns>
        ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<IAsyncFeatureProcessingPipeline<T>, string, Task<TResult>> processor);
    }
}