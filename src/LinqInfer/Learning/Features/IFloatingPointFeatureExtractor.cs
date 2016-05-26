using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Interface for extracting features from an object type as an array of 
    /// single precision floating point numbers.
    /// </summary>
    public interface IFloatingPointFeatureExtractor<T> : IFeatureExtractor<T, double>
    {
        /// <summary>
        /// Extracts a column vector which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        ColumnVector1D ExtractColumnVector(T obj);
    }
}
