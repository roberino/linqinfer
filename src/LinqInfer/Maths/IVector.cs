using LinqInfer.Data;
using System;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a vector.
    /// There are numerous implementations of a 
    /// vector some of which are more efficient in
    /// specific scenarios
    /// </summary>
    public interface IVector : IEquatable<IVector>, IBinaryExportable
    {
        /// <summary>
        /// Returns the size of the vector
        /// </summary>
        int Size { get; }

        /// <summary>
        /// Returns the sum of all values
        /// </summary>
        double Sum { get; }

        /// <summary>
        /// Returns the value at an index
        /// </summary>
        double this[int index] { get; }

        /// <summary>
        /// Returns the matrix multiplied by this vector
        /// i.e. M X V
        /// </summary>
        IVector MultiplyBy(Matrix matrix);

        /// <summary>
        /// Returns the given vector multiplied by this vector
        /// </summary>
        IVector MultiplyBy(IVector vector);

        /// <summary>
        /// Returns the vector (as a row matrix) multiplied
        /// by the matrix (V X M)
        /// </summary>
        IVector HorizontalMultiply(IMatrix matrix);

        /// <summary>
        /// Returns the dot product of this vector and another
        /// </summary>
        double DotProduct(IVector vector);

        /// <summary>
        /// Converts or casts the vector as a column vector
        /// </summary>
        /// <returns></returns>
        ColumnVector1D ToColumnVector();
    }
}