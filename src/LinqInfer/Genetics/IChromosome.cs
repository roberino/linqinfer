using System;

namespace LinqInfer.Genetics
{
    public interface IChromosome<T> : IEquatable<T>
    {
        T Breed(T other);
    }
}
