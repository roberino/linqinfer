using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Sampling
{
    [Serializable]
    public class SampleSummary
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double StdDev { get; set; }
        public double Mean { get; set; }
        public int Count { get; set; }
        public int Size { get; set; }

        internal virtual void Recalculate(IEnumerable<DataItem> SampleData)
        {
            var data = SampleData.Select(d => d.AsColumnVector()).ToList();

            Min = data.Select(x => x.EuclideanLength).Min();
            Max = data.Select(x => x.EuclideanLength).Max();

            var muStdDev = Functions.MeanStdDev(data.Select(x => x.EuclideanLength));

            Mean = muStdDev.Item1;
            StdDev = muStdDev.Item2;
            Count = SampleData.Count();

            if (Count > 0)
            {
                var fv = SampleData.First().FeatureVector;
                Size = fv == null ? 0 : fv.Length;
            }
        }
    }
}
