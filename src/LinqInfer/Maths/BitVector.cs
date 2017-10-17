using System;
using System.Diagnostics.Contracts;

namespace LinqInfer.Maths
{
    public class BitVector
    {
        private readonly byte[] _data;

        public BitVector(bool[] values)
        {
            _data = Encode(values);
            Size = values.Length;
        }

        private BitVector(byte[] data, int size)
        {
            if (data.Length / 8 > size) throw new ArgumentOutOfRangeException(nameof(size));

            _data = data;
            Size = size;
        }

        /// <summary>
        /// Gets the size of the vector
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Retrieves a value by index
        /// </summary>
        public bool this[int index]
        {
            get
            {
                var mask = (byte)(1 << (index % 8));
                return (_data[index / 8] & mask) > 0;
            }
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

            byte mask = 1;

            for (var i = 0; i < values.Length; i++)
            {
                if (values[i]) data[i / 8] |= mask;
                mask = (byte)(mask << 1);
            }

            return data;
        }
    }
}