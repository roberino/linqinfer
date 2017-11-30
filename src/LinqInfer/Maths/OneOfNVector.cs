using LinqInfer.Data;
using LinqInfer.Utility;
using System;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class OneOfNVector : IVector, ICloneableObject<OneOfNVector>
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

        public double Sum => ActiveIndex.HasValue ? 1 : 0;

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

        public IVector MultiplyBy(Matrix matrix)
        {
            var result = new double[Size];

            if (ActiveIndex.HasValue)
            {
                result[ActiveIndex.Value] = matrix.ColumnSum(ActiveIndex.Value);
            }

            return new ColumnVector1D(result);
        }

        public IVector MultiplyBy(IVector vector)
        {
            ArgAssert.AssertEquals(vector.Size, Size, nameof(Size));

            if (vector is OneOfNVector) return ((OneOfNVector)vector) * this;
            if (vector is BitVector) return ((BitVector)vector).MultiplyBy(this);

            var result = new double[Size];

            if (ActiveIndex.HasValue)
            {
                result[ActiveIndex.Value] = vector[ActiveIndex.Value];

                return new ColumnVector1D(result);
            }

            return new ZeroVector(Size);
        }

        public static IVector operator *(OneOfNVector vector1, OneOfNVector vector2)
        {
            ArgAssert.AssertEquals(vector1.Size, vector2.Size, nameof(vector1.Size));

            if (vector1.ActiveIndex == vector2.ActiveIndex) return vector1.Clone(true);

            return new ZeroVector(vector1.Size);
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

            if (ActiveIndex.HasValue) return other[ActiveIndex.Value] == 1;

            return other.ToColumnVector().IsZero;
        }

        public OneOfNVector Clone(bool deep)
        {
            return new OneOfNVector(Size, ActiveIndex);
        }

        public byte[] ToByteArray()
        {
            var size = BitConverter.GetBytes(Size);
            var index = BitConverter.GetBytes(ActiveIndex.GetValueOrDefault(-1));

            return size.Concat(index).ToArray();
        }

        public static OneOfNVector FromByteArray(byte[] data)
        {
            var size = BitConverter.ToInt32(data, 0);
            var index = BitConverter.ToInt32(data, sizeof(int));

            return new OneOfNVector(size, index > -1 ? new int?(index) : null);
        }
    }
}