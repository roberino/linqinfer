using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    internal class Histogram : IDensityEstimationStrategy<Fraction>
    {
        private readonly double? _width;

        public Histogram(double? width = null)
        {
            if (width.HasValue && width.Value < 0)
            {
                throw new ArgumentOutOfRangeException(string.Format("Must have a width > 0. Value supplied = {0}", width.Value));
            }

            _width = width;
        }

        public DiscretizedSet<double, double, Fraction> Analyse(IQueryable<Fraction> sample)
        {
            return Analyse(sample, x => x.Value);
        }

        public DiscretizedSet<DateTime, TimeSpan, T> Analyse<T>(IQueryable<T> sample, Expression<Func<T, DateTime>> valueFunction)
        {
            if (!sample.Any())
            {
                return new DiscretizedSet<DateTime, TimeSpan, T>(DateTime.MinValue, DateTime.MinValue, TimeSpan.MinValue, x => DateTime.MinValue);
            }

            var min = sample.Select(valueFunction).Min();
            var max = sample.Select(valueFunction).Max();
            var span = max - min;

            TimeSpan h; // width of bin
            int k; // N of bins

            if (_width.HasValue)
            {
                h = TimeSpan.FromMilliseconds(_width.Value);
                k = (int)Math.Ceiling(span.TotalMilliseconds / _width.Value);
            }
            else
            {
                var n = sample.Count();
                k = (int)Math.Ceiling(Math.Log(n, 2) + 1);
                h = TimeSpan.FromMilliseconds(span.TotalMilliseconds / k);
            }

            var results = new DiscretizedSet<DateTime, TimeSpan, T>(min, max, h, w => min + TimeSpan.FromMilliseconds(w * h.TotalMilliseconds));

            results.CreateBins(Enumerable.Range(0, k + 1));

            var vf = valueFunction.Compile();

            foreach (var s in sample.AsEnumerable().Select(x => new
            {
                item = x,
                value = vf(x)
            }))
            {
                var bin = (int)((s.value - min).TotalMilliseconds / h.TotalMilliseconds);

                results.AddValue(bin, s.item);
            }

            return results;
        }

        public DiscretizedSet<double, double, T> Analyse<T>(IQueryable<T> sample, Expression<Func<T, double>> valueFunction)
        {
            if (!sample.Any())
            {
                return new DiscretizedSet<double, double, T>(0, 0, 0, x => 0);
            }

            var min = sample.Select(valueFunction).Min();
            var max = sample.Select(valueFunction).Max();
            var span = max - min;

            double h; // width of bin
            int k; // N of bins

            if (_width.HasValue)
            {
                h = _width.Value;
                k = (int)Math.Ceiling(span / h);
            }
            else
            {
                var n = sample.Count();
                k = (int)Math.Ceiling(Math.Log(n, 2) + 1);
                h = span / k;
            }

            var results = new DiscretizedSet<double, double, T>(min, max, h, w => min + w * h);

            results.CreateBins(Enumerable.Range(0, k + 1));

            var vf = valueFunction.Compile();

            foreach (var s in sample.AsEnumerable().Select(x => new
            {
                item = x,
                value = vf(x)
            }))
            {
                var bin = (int)((s.value - min) / h);

                //var bin = Math.Max(Math.Min((int)Math.Floor((item.Value - min) / h), k - 1), 0);

                results.AddValue(bin, s.item);
            }

            return results;
        }

        public Func<Fraction, Fraction> Evaluate(IQueryable<Fraction> sample)
        {
            var sampleBins = Analyse(sample);

            return (x) =>
            {
                int k = (int)Math.Floor(((x.Value - sampleBins.Min) / sampleBins.Width));
                int f = 0;
                if (sampleBins.Bins.TryGetValue(k, out f))
                {
                    return new Fraction(f, sampleBins.Total);
                }
                return Fraction.Zero;
            };
        }
    }
}