using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface IVectorExtractor<T>
    {
        /// <summary>
        /// Extracts a column vector which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        IVector ExtractIVector(T obj);
    }
}