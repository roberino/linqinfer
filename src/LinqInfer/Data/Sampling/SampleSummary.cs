using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data.Sampling
{
    public class SampleSummary : IEntity
    {
        public long Id { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }
        public double StdDev { get; set; }
        public double Mean { get; set; }
        public int Count { get; set; }
        public int Size { get; set; }

        internal virtual void Recalculate(IEnumerable<DataItem> SampleData)
        {
            Count = SampleData.Count();

            if (Count > 0)
            {
                var data = SampleData.Select(d => d.AsColumnVector()).ToList();

                Min = data.Select(x => x.EuclideanLength).Min();
                Max = data.Select(x => x.EuclideanLength).Max();

                var muStdDev = Functions.MeanStdDev(data.Select(x => x.EuclideanLength));

                Mean = muStdDev.Item1;
                StdDev = muStdDev.Item2;
                var fv = SampleData.First().FeatureVector;
                Size = fv == null ? 0 : fv.Length;
            }
        }
    }
}
