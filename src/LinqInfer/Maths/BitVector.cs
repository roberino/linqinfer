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

        public BitVector(bool[] values)
        {
            ArgAssert.AssertNonNull(values, nameof(values));

            _data = Encode(values);
            Size = values.Length;
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
        /// Retrieves a double value by index
        /// </summary>
        public double this[int index]
        {
            get
            {
                var mask = (byte)(1 << (index % 8));
                return (_data[index / 8] & mask);
            }
        }

        /// <summary>
        /// Retrieves a value by index
        /// </summary>
        public bool ValueAt(int index) => this[index] > 0;

        public IEnumerator<bool> GetEnumerator()
        {
            return Enumerable.Range(0, Size).Select(i => ValueAt(i)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ColumnVector1D Multiply(Matrix matrix)
        {
            return matrix * this;
        }

        public ColumnVector1D Multiply(IVector vector)
        {
            var result = new double[Size];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ValueAt(i) ? vector[i] : 0;
            }

            return new ColumnVector1D(result);
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
            var vals = new byte[size / 8];

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

            var data = new byte[values.Length / 8 + 1];
            
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i]) data[i / 8] |= (byte)(1 << (i % 8));
            }

            return data;
        }
    }
}