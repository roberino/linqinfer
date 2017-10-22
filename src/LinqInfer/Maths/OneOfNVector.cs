using LinqInfer.Utility;
using System;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class OneOfNVector : IVector
    {
        public OneOfNVector(int size, int? activeIndex = null)
        {
            ArgAssert.AssertGreaterThanZero(size, nameof(size));
            ArgAssert.AssertGreaterThanOrEqualToZero(activeIndex.GetValueOrDefault(0), nameof(activeIndex));
            ArgAssert.AssertLessThan(activeIndex.GetValueOrDefault(-1), size, nameof(activeIndex));

            Size = size;
            ActiveIndex = activeIndex;
        }

        public int Size { get; }

        public int? ActiveIndex { get; }

        public double this[int index] => ActiveIndex.HasValue && ActiveIndex.Value == index ? 1d : 0;

        public BitVector ToBitVector()
        {
            return new BitVector(Enumerable.Range(0, Size).Select(n => n == ActiveIndex).ToArray());
        }

        public ColumnVector1D ToColumnVector()
        {
            var result = new double[Size];

            if (ActiveIndex.HasValue)
            {
                result[ActiveIndex.Value] = 1;
            }

            return new ColumnVector1D(result);
        }

        public ColumnVector1D Multiply(Matrix matrix)
        {
            var result = new double[Size];

            if (ActiveIndex.HasValue)
            {
                result[ActiveIndex.Value] = matrix.ColumnSum(ActiveIndex.Value);
            }

            return new ColumnVector1D(result);
        }

        public ColumnVector1D Multiply(IVector vector)
        {
            var result = new double[Size];

            if (ActiveIndex.HasValue)
            {
                result[ActiveIndex.Value] = vector[ActiveIndex.Value];
            }

            return new ColumnVector1D(result);
        }

        public double DotProduct(IVector vector)
        {
            if (ActiveIndex.HasValue)
            {
                return vector[ActiveIndex.Value];
            }

            return 0d;
        }

        public override string ToString()
        {
            return ToColumnVector().ToString();
        }

        public override int GetHashCode()
        {
            return new Tuple<int, int>(Size, ActiveIndex.GetValueOrDefault(-1)).GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVector);
        }

        public bool Equals(IVector other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.Size != Size) return false;
            if (other is OneOfNVector) return ((OneOfNVector)other).ActiveIndex == ActiveIndex;

            return other.ToColumnVector().Equals(ToColumnVector());
        }
    }
}