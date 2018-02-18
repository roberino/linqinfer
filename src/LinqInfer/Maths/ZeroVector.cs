using LinqInfer.Utility;
using System;

namespace LinqInfer.Maths
{
    public class ZeroVector : IVector
    {
        public ZeroVector(int size)
        {
            Size = size;
        }

        public double this[int index] => 0;

        public int Size { get; }

        public double Sum => 0;

        public double DotProduct(IVector vector)
        {
            ArgAssert.AssertEquals(vector.Size, Size, nameof(Size));

            return 0;
        }

        public bool Equals(IVector other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.Size != Size) return false;
            if (other is ZeroVector) return true;
            return ToColumnVector().Equals(other);
        }

        public IVector MultiplyBy(Matrix matrix)
        {
            ArgAssert.AssertEquals(matrix.Width, Size, nameof(matrix.Width));

            return new ZeroVector(matrix.Height);
        }

        public IVector MultiplyBy(IVector vector)
        {
            ArgAssert.AssertEquals(vector.Size, Size, nameof(Size));

            return new ZeroVector(Size);
        }

        public IVector HorizontalMultiply(IMatrix matrix)
        {
            ArgAssert.AssertEquals(Size, matrix.Height, nameof(matrix.Height));

            return new ZeroVector(matrix.Width);
        }

        public byte[] ToByteArray()
        {
            return BitConverter.GetBytes(Size);
        }

        public static ZeroVector FromByteArray(byte[] bytes)
        {
            return new ZeroVector(BitConverter.ToInt32(bytes, 0));
        }

        public ColumnVector1D ToColumnVector()
        {
            return Vector.UniformVector(Size, 0).ToColumnVector();
        }
    }
}