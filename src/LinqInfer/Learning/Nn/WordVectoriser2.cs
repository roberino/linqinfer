using LinqInfer.Learning.Features;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class WordVectoriser2 : IByteFeatureExtractor<string>
    {
        public WordVectoriser2()
        {
            int i = 0;
            Labels = Enumerable.Range((int)'A', 26).ToDictionary(b => ((char)b).ToString(), b => i++);
            Labels["Length"] = 27;
            Labels["Check Sum"] = 28;
            Labels["Null 1"] = 29;
            Labels["Null 2"] = 30;
            Labels["Null 3"] = 31;
        }

        public IDictionary<string, int> Labels { get; private set; }

        public int VectorSize { get { return 32; } }

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