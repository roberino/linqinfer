using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    /// <summary>
    /// 1 dimentional KDE
    /// </summary>
    class MultiVariateKernelDensityEstimator : IDensityEstimationStrategy<IVector>
    {
        readonly float _bandwidth;

        public MultiVariateKernelDensityEstimator(
            float bandwidth = 0.2F)
        {
            _bandwidth = (float)ArgAssert.AssertGreaterThanZero(bandwidth, nameof(bandwidth));
        }

        public Func<IVector, Fraction> Evaluate(IQueryable<IVector> sample)
        {
            var f = MultiVariateNormalKernel(sample, _bandwidth);
            return x => Fraction.ApproximateRational(f(x));
        }

        public IDictionary<ColumnVector1D, double> CreateMultiVariateDistribution(IQueryable<ColumnVector1D> sample, int binCount = 10)
        {
            return CreateMultiVariateDistributionInternal(sample, binCount).ToDictionary(v => v.Key, v => v.Value);
        }

        internal static Func<IVector, double> MultiVariateNormalKernel(IEnumerable<IVector> sample, double bandwidth)
        {
            var h = bandwidth;
            var hSq2 = 2 * h * h;
            var n = (double)sample.Count();
            var a = 1d / n;

            return (v) =>
            {
                var x = v.ToColumnVector();
                var b = 1d / Math.Pow(Math.PI * hSq2, x.Size / 2);
                var c = sample.Select(s => s.ToColumnVector()).Select(x0 => b * Math.Exp(-Math.Pow((x - x0).EuclideanLength, 2) / hSq2)).Sum();

                return a * c;
            };
        }

        IEnumerable<KeyValuePair<ColumnVector1D, double>> CreateMultiVariateDistributionInternal(IQueryable<ColumnVector1D> sample, int binCount = 10)
        {
            var f = MultiVariateNormalKernel(sample, _bandwidth);
            var min = sample.MinOfEachDimension();
            var max = sample.MaxOfEachDimension();
            
            foreach(var item in min.Range(max, binCount))
            {
                yield return new KeyValuePair<ColumnVector1D, double>(item, f(item));
            }
        }
    }
}