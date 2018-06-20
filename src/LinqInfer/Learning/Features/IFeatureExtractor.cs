using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractor<in TInput, out TVector> : IHasFeatureMetadata, IBinaryPersistable where TVector : struct
    {
        /// <summary>
        /// The size of the feature vector.
        /// </summary>
        int VectorSize { get; }

        /// <summary>
        /// Extracts an array of primitive values which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        TVector[] ExtractVector(TInput obj);
    }
}