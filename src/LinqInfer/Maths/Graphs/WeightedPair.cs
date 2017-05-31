using System;

namespace LinqInfer.Maths.Graphs
{
    public sealed class WeightedPair<T, C> where C : IComparable<C>
    {
        internal WeightedPair(T value, C weight)
        {
            Value = value;
            Weight = weight;
        }

        public T Value { get; private set; }

        public C Weight { get; private set; }
    }
}