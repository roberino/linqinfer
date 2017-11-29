using LinqInfer.Utility;
using System;

namespace LinqInfer.Maths
{
    internal class PartialVector : IVector
    {
        private readonly double[] _data;
        private readonly int _startIndex;

        public PartialVector(double[] data, int startIndex, int size)
        {
            _data = ArgAssert.AssertNonNull(data, nameof(data));
            _startIndex = ArgAssert.AssertGreaterThanOrEqualToZero(startIndex, nameof(startIndex));

            ArgAssert.Assert(() => size <= data.Length - startIndex, nameof(size));

            Size = size;
        }

        public double this[int index]
        {
            get
            {
                ArgAssert.AssertGreaterThanOrEqualToZero(index, nameof(index));

                if (index >= Size) throw new IndexOutOfRangeException(index.ToString());

                return _data[index - _startIndex];
            }
        }

        public int Size { get; }

        public double DotProduct(IVector vector)
        {
            return MultiplyBy(vector).ToColumnVector().Sum();
        }

        public bool Equals(IVector other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.Size != Size) return false;

            for (int i = _startIndex; i < Size; i++)
            {
                if (_data[i] != other[i - _startIndex])
                {
                    return false;
                }
            }

            return true;
        }

        public IVector MultiplyBy(Matrix matrix)
        {
            return ToColumnVector().MultiplyBy(matrix);
        }

        public IVector MultiplyBy(IVector vector)
        {
            ArgAssert.AssertEquals(vector.Size, Size, nameof(Size));

            var values = new double[Size];
            var endIndex = (_startIndex + Size);

            for (int i = _startIndex; i < endIndex; i++)
            {
                values[i - _startIndex] = _data[i] * vector[i - _startIndex];
            }

            return new ColumnVector1D(values);
        }

        public byte[] ToByteArray()
        {
            return ToColumnVector().ToByteArray();
        }

        public ColumnVector1D ToColumnVector()
        {
            var values = new double[Size];

            Array.Copy(_data, _startIndex, values, 0, Size);

            return new ColumnVector1D(values);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVector);
        }

        public override int GetHashCode()
        {
            return ToColumnVector().GetHashCode();
        }

        public override string ToString()
        {
            return ToColumnVector().ToCsv(3);
        }
    }
}