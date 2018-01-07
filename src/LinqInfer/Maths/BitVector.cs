using LinqInfer.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public class BitVector : IEnumerable<bool>, IVector
    {
        private readonly byte[] _data;

        public BitVector(params bool[] values)
        {
            ArgAssert.AssertNonNull(values, nameof(values));

            _data = Encode(values);
            Size = values.Length;
        }

        public BitVector(IEnumerable<int> hotIndexes, int vectorSize)
        {
            ArgAssert.AssertNonNull(hotIndexes, nameof(hotIndexes));
            ArgAssert.AssertGreaterThanOrEqualToZero(vectorSize, nameof(vectorSize));

            _data = Encode(hotIndexes, vectorSize);
            Size = vectorSize;
        }

        private BitVector(byte[] data, int size)
        {
            ArgAssert.AssertNonNull(data, nameof(data));
            ArgAssert.AssertLessThan(data.Length / 8, size, nameof(size));

            _data = data;
            Size = size;
        }

        /// <summary>
        /// Gets the size of the vector
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the sum of all values
        /// </summary>
        public double Sum => this.Sum(v => v ? 1 : 0);

        /// <summary>
        /// Retrieves a double value by index
        /// </summary>
        public double this[int index]
        {
            get
            {
                var mask = (byte)(1 << (index % 8));
                return (_data[index / 8] & mask) == 0 ? 0 : 1;
            }
        }

        /// <summary>
        /// Retrieves a value by index
        /// </summary>
        public bool ValueAt(int index) => this[index] != 0;

        public IEnumerator<bool> GetEnumerator()
        {
            return Enumerable.Range(0, Size).Select(i => ValueAt(i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IVector MultiplyBy(Matrix matrix)
        {
            return matrix * this;
        }

        public IVector MultiplyBy(IVector vector)
        {
            if (vector is BitVector) return (((BitVector)vector) * this);

            var result = new double[Size];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ValueAt(i) ? vector[i] : 0;
            }

            return new ColumnVector1D(result);
        }

        public static BitVector operator *(BitVector v1, BitVector v2)
        {
            ArgAssert.AssertEquals(v1.Size, v2.Size, nameof(v1.Size));

            var data = new byte[v1._data.Length];

            for (int i = 0; i < v1._data.Length; i++)
            {
                data[i] = (byte)(v1._data[i] & v2._data[i]);
            }

            return new BitVector(data, v2.Size);
        }

        public double DotProduct(IVector vector)
        {
            var result = 0d;

            for (int i = 0; i < vector.Size; i++)
            {
                result += ValueAt(i) ? vector[i] : 0;
            }

            return result;
        }

        public static ColumnVector1D operator *(Matrix m, BitVector v)
        {
            var result = new double[v.Size];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = v.ValueAt(i) ? m.ColumnSum(i) : 0;
            }

            return new ColumnVector1D(result);
        }

        /// <summary>
        /// Converts the bit vector to a columng vector
        /// </summary>
        public ColumnVector1D ToColumnVector()
        {
            var result = new double[Size];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = this[i];
            }

            return new ColumnVector1D(result);
        }

        /// <summary>
        /// Returns a byte array containing the data
        /// </summary>
        public byte[] ToByteArray()
        {
            var len = BitConverter.GetBytes(Size);
            var arr = new byte[_data.Length + len.Length];

            Array.Copy(len, arr, len.Length);
            Array.Copy(_data, 0, arr, len.Length, _data.Length);

            return arr;
        }

        public static BitVector FromByteArray(byte[] data)
        {
            var len = BitConverter.GetBytes(0);

            Array.Copy(data, 0, len, 0, len.Length);

            var size = BitConverter.ToInt32(len, 0);
            var vals = GetByteStore(size);

            Array.Copy(data, len.Length, vals, 0, vals.Length);

            return new BitVector(vals, size);
        }

        /// <summary>
        /// Converts the vector to a Base64 string
        /// </summary>
        public string ToBase64()
        {
            return Convert.ToBase64String(ToByteArray());
        }

        /// <summary>
        /// Converts the vector from a Base64 string
        /// </summary>
        public static BitVector FromBase64(string data)
        {
            return FromByteArray(Convert.FromBase64String(data));
        }

        private static byte[] Encode(bool[] values)
        {
            Contract.Requires(values != null);

            var data = GetByteStore(values.Length);
            
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i]) data[i / 8] |= (byte)(1 << (i % 8));
            }

            return data;
        }

        private static byte[] Encode(IEnumerable<int> hotIndexes, int vectorSize)
        {
            Contract.Requires(hotIndexes != null);

            var data = GetByteStore(vectorSize);

            foreach(var i in hotIndexes)
            {
                data[i / 8] |= (byte)(1 << (i % 8));
            }

            return data;
        }

        private static byte[] GetByteStore(int len)
        {
            return new byte[len / 8 + 1];
        }

        public override string ToString()
        {
            return ToColumnVector().ToString();
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IVector);
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(_data);
        }

        public bool Equals(IVector other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (other.Size != Size) return false;

            if (other is BitVector)
            {
                return StructuralComparisons.StructuralEqualityComparer.Equals(_data, ((BitVector)other)._data);
            }

            return other.ToColumnVector().Equals(ToColumnVector());
        }
    }
}