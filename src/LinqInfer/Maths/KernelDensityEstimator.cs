using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    /// <summary>
    /// 1 dimentional KDE
    /// </summary>
    internal class KernelDensityEstimator : IDensityEstimationStrategy<Fraction>, IDensityEstimationStrategy<ColumnVector1D>
    {
        private readonly Func<IQueryable<Fraction>, Func<Fraction, Fraction>> _kernelFact;
        private readonly float _bandwidth;

        public KernelDensityEstimator(
            float bandwidth = 0.2F)
        {
            Contract.Assert(bandwidth > 0);

            _bandwidth = bandwidth;

            _kernelFact = (s) =>
            {
                var muStdDev = Functions.MeanStdDev(s);

                return x => Functions.NormalDistribution(x, muStdDev.Item2, muStdDev.Item1);
            };
        }

        public KernelDensityEstimator(
            Func<IQueryable<Fraction>, Func<Fraction, Fraction>> kernelFact,
            float bandwidth = 0.2F)
        {
            Contract.Assert(bandwidth > 0);

            _bandwidth = bandwidth;
            _kernelFact = kernelFact;
        }

        public Func<Fraction, Fraction> Evaluate(IQueryable<Fraction> sample)
        {
            // h = bandwidth (smoothing param)
            // n = count of sample data
            // kF = kernel function
            // Sum((1 / hn) kF((y - x[i]) / h)) [over sample data]
            var h = Fraction.ApproximateRational(_bandwidth, 16);
            var n = sample.Count();
            var hn = Fraction.ApproximateRational(_bandwidth * n);
            var max = sample.OrderByDescending(s => s.Value).First();
            var hReciprocal = Fraction.One / hn;
            var kF = _kernelFact(sample);

            return new Func<Fraction, Fraction>(y =>
            {
                //return kF(y);
                var t = sample.Select(x => kF(Fraction.Divide(Fraction.Add(y, -x, true), h, true)));
                var fpSum = t.Sum(v => v.Value);
                var norm = hReciprocal.Value * fpSum;
                var normR = Fraction.ApproximateRational(norm);

                return Fraction.Divide(normR, max, true);
            });
        }

        public Func<ColumnVector1D, Fraction> Evaluate(IQueryable<ColumnVector1D> sample)
        {
            var f = Functions.MultiVariateNormalKernel(sample, _bandwidth);
            return x => Fraction.ApproximateRational(f(x));
        }

        public IDictionary<ColumnVector1D, double> CreateMultiVariateDistribution(IQueryable<ColumnVector1D> sample, int binCount = 10)
        {
            return CreateMultiVariateDistributionInternal(sample, binCount).ToDictionary(v => v.Key, v => v.Value);
        }

        private IEnumerable<KeyValuePair<ColumnVector1D, double>> CreateMultiVariateDistributionInternal(IQueryable<ColumnVector1D> sample, int binCount = 10)
        {
            var f = Functions.MultiVariateNormalKernel(sample, _bandwidth);
            var min = sample.MinOfEachDimension();
            var max = sample.MaxOfEachDimension();
            
            foreach(var item in min.Range(max, binCount))
            {
                yield return new KeyValuePair<ColumnVector1D, double>(item, f(item));
            }
        }
    }
}
