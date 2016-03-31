using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class WordVectoriser : IByteFeatureExtractor<string>
    {
        private readonly int _size;

        public WordVectoriser(int maxWordSize)
        {
            _size = maxWordSize;
            Labels = Enumerable.Range(0, _size).ToDictionary(n => "Char " + n, n => n);
        }

        public IDictionary<string, int> Labels { get; private set; }

        public int VectorSize { get { return _size; } }

        public byte[] CreateNormalisingVector(string sample = null)
        {
            return new byte[_size];
        }

        public byte[] ExtractVector(string data)
        {
            var arr = new byte[_size];

            for (int i = 0; i < System.Math.Min(data.Length, _size); i++)
            {
                arr[i] = (byte)data[i];
            }

            return arr;
        }
    }
}
