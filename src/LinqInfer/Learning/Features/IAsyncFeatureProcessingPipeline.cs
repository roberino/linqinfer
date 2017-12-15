using LinqInfer.Data.Pipes;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Represents an asyncronous pipeline of feature data
    /// which can be transformed and processed
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    public interface IAsyncFeatureProcessingPipeline<T> : IAsyncPipe<ObjectVector<T>> where T : class
    {
        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }

        /// <summary>
        /// Returns an enumeration of vector data in batches.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<ObjectVector<T>> ExtractBatches();

        /// <summary>
        /// Preprocesses the data with the vector transformation
        /// </summary>
        IAsyncFeatureProcessingPipeline<T> PreprocessWith(IVectorTransformation transformation);
    }
}