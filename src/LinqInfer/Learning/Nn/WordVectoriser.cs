using LinqInfer.Learning.Features;
using System;

namespace LinqInfer.Learning
{
    public class WordVectoriser : IByteFeatureExtractor<string>
    {
        private readonly int _size;

        public WordVectoriser(int maxWordSize)
        {
            _size = maxWordSize;
        }

        public int VectorSize { get { return _size; } }

        public byte[] CreateNormalisingVector(string sample = null)
        {
            return new byte[_size];
        }

        public byte[] ExtractVector(string data)
        {
            var arr = new byte[_size];

            for (int i = 0; i < Math.Min(data.Length, _size); i++)
            {
                arr[i] = (byte)data[i];
            }

            return arr;
        }
    }
}
