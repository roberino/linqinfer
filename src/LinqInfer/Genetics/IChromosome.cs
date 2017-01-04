using System;

namespace LinqInfer.Genetics
{
    /// <summary>
    /// Basic interface for supporting "breedability"
    /// to allow for an object to be mutated in order to find an optimal value
    /// </summary>
    /// <typeparam name="T">The underlying type</typeparam>
    public interface IChromosome<T> : IEquatable<T>
    {
        T Breed(T other);
    }
}
