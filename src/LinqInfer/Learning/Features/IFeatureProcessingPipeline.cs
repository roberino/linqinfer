using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureProcessingPipeline<T> : IFeatureDataSource, IFeatureTransformBuilder<T> where T : class
    {
        /// <summary>
        /// Returns the input objects
        /// </summary>
        IQueryable<T> Data { get; }

        /// <summary>
        /// Normalised the feature data (if normalisation is enabled on the feature extractor).
        /// </summary>
        IFeatureProcessingPipeline<T> NormaliseData();

        /// <summary>
        /// Returns an enumeration of vector data in batches.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IList<ObjectVector<T>>> ExtractBatches(int batchSize = 1000);
    }
}
