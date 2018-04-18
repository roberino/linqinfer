using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Represents an asyncronous pipeline of feature data
    /// which can be transformed and processed
    /// </summary>
    /// <typeparam name="T">The input type</typeparam>
    public interface IAsyncFeatureProcessingPipeline<T> : IAsyncPipe<ObjectVectorPair<T>> where T : class
    {
        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }

        /// <summary>
        /// Returns an enumeration of vector data in batches.
        /// </summary>
        /// <returns></returns>
        IAsyncEnumerator<ObjectVectorPair<T>> ExtractBatches();

        /// <summary>
        /// Preprocesses the data with the vector transformation
        /// </summary>
        IAsyncFeatureProcessingPipeline<T> PreprocessWith(ISerialisableVectorTransformation transformation);

        /// <summary>
        /// Centres and scales the data
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Task<IAsyncFeatureProcessingPipeline<T>> CentreAndScaleAsync(Range? range = null);
    }
}