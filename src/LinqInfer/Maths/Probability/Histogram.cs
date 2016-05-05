using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    internal class Histogram : IDensityEstimationStrategy<Fraction>
    {
        private readonly double? _width;

        public Histogram(double? width = null)
        {
            if(width.HasValue && width.Value < 0)
            {
                throw new ArgumentOutOfRangeException(string.Format("Must have a width > 0. Value supplied = {0}", width.Value));
            }

            _width = width;
        }

        public ResultSet Analyse(IQueryable<Fraction> sample)
        {
            var min = sample.Select(x => x.Value).Min();
            var max = sample.Select(x => x.Value).Max();
            var span = max - min;

            double h; // width of bin
            int k; // N of bins

            if (_width.HasValue)
            {
                h = _width.Value;
                k = (int)System.Math.Ceiling(span / h);
            }
            else {
                var n = sample.Count();
                k = (int)System.Math.Ceiling(System.Math.Log(n, 2) + 1);
                h = span / k;
            }

            var hist = Enumerable.Range(0, k + 1).ToDictionary(n => n, n => 0);

            foreach (var item in sample)
            {
                var bin = (int)((item.Value - min) / h);

                //var bin = Math.Max(Math.Min((int)Math.Floor((item.Value - min) / h), k - 1), 0);

                hist[bin]++;
            }

            return new ResultSet() { Min = min, Width = h, Bins = hist, Total = hist.Values.Sum() };
        }

        public Func<Fraction, Fraction> Evaluate(IQueryable<Fraction> sample)
        {
            var sampleBins = Analyse(sample);
            var hx = sampleBins.Width / 2d;

            return (x) =>
            {
                int k = (int)System.Math.Floor(((x.Value - sampleBins.Min) / sampleBins.Width));
                int f = 0;
                if (sampleBins.Bins.TryGetValue(k, out f))
                {
                    return new Fraction(f, sampleBins.Total);
                }
                return Fraction.Zero;
            };
        }

        public class ResultSet
        {
            public double Min { get; internal set; }

            public double Width { get; internal set; }

            public int Total { get; internal set; }

            public IDictionary<int, int> Bins { get; internal set; }
        }
    }
}
