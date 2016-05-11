using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class SingleWordVectoriser : IByteFeatureExtractor<string>
    {
        private readonly int _size;

        public SingleWordVectoriser(int maxWordSize)
        {
            _size = maxWordSize;
            IndexLookup = Enumerable.Range(0, _size).ToDictionary(n => "Char " + n, n => n);
            FeatureMetadata = Feature.CreateDefault(IndexLookup.Keys, DistributionModel.Categorical);
        }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize { get { return _size; } }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public byte[] NormaliseUsing(IEnumerable<string> samples)
        {
            return CreateNormalisingVector();
        }

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
