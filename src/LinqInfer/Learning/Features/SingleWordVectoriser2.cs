using LinqInfer.Learning.Features;
using System.Collections.Generic;
using System.Linq;
using System;
using LinqInfer.Maths.Probability;

namespace LinqInfer.Learning.Features
{
    internal class SingleWordVectoriser2 : IByteFeatureExtractor<string>
    {
        public SingleWordVectoriser2()
        {
            int i = 0;
            IndexLookup = Enumerable.Range((int)'A', 26).ToDictionary(b => ((char)b).ToString(), b => i++);
            IndexLookup["Length"] = 27;
            IndexLookup["Check Sum"] = 28;
            IndexLookup["Null 1"] = 29;
            IndexLookup["Null 2"] = 30;
            IndexLookup["Null 3"] = 31;
            FeatureMetadata = Feature.CreateDefault(IndexLookup.Keys, DistributionModel.Unknown);
        }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public int VectorSize { get { return 32; } }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public byte[] NormaliseUsing(IEnumerable<string> samples)
        {
            return CreateNormalisingVector();
        }

        public byte[] CreateNormalisingVector(string sample = null)
        {
            return new byte[VectorSize];
        }

        public byte[] ExtractVector(string data)
        {
            var vector = new byte[VectorSize];
            var a = 'a';
            var z = 'z';

            if (data != null)
            {
                byte chksum = 0;

                foreach(var c in data.ToLower())
                {
                    if(c >= a && c <= z)
                    {
                        vector[c - a] = 255;
                    }
                    else
                    {
                        vector[26] = 255;
                    }

                    chksum &= (byte)c;
                }

                vector[27] = (byte)(data.Length / 15 * 255);
                vector[28] = chksum;
            }

            return vector;
        }
    }
}