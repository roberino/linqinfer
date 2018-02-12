using System;

namespace LinqInfer.Maths
{
    /// <summary>
    /// Represents a vector that can be modified
    /// </summary>
    public interface IMutableVector : IVector
    {
        /// <summary>
        /// Applies a function over all values in the vector.
        /// </summary>
        /// <param name="func">The function takes the existing value, the index
        /// and returns a new value</param>
        void Apply(Func<double, int, double> func);
    }
}