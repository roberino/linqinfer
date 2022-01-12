using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Interface for extracting features from an object type as an array of 
    /// single precision floating point numbers.
    /// </summary>
    public interface IVectorFeatureExtractor<in T> : IHasFeatureMetadata, IExportableAsDataDocument
    {
        /// <summary>
        /// The size of the feature vector.
        /// </summary>
        int VectorSize { get; }

        /// <summary>
        /// Returns true if the extractor can produce a non-zero vector from the object
        /// </summary>
        bool CanEncode(T obj);

        /// <summary>
        /// Extracts a column vector which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        IVector ExtractIVector(T obj);
    }
}
