using System;
using System.Linq;

namespace LinqInfer.Probability
{
    class KernelDensityEstimator : IKernelDensityEstimator<Fraction>
    {
        private readonly Func<IQueryable<Fraction>, Func<Fraction, Fraction>> _kernelFact;

        public KernelDensityEstimator()
        {
            _kernelFact = (s) =>
            {
                var muStdDev = Functions.MeanStdDev(s);

                return x => Functions.NormalDistribution(x, muStdDev.Item2, muStdDev.Item1);
            };
        }

        public KernelDensityEstimator(Func<IQueryable<Fraction>, Func<Fraction, Fraction>> kernelFact)
        {
            _kernelFact = kernelFact;
        }

        public Func<Fraction, Fraction> Evaluate(IQueryable<Fraction> sample,
            float bandwidth = 0.2F)
        {
            // h = bandwidth
            // Sum(K(y - x[i]) / h) [over sample data]
            var h = Fraction.ApproximateRational(bandwidth);
            var k = _kernelFact(sample);

            return new Func<Fraction, Fraction>(y =>
            {
                var t = sample.Select(x => k(y - x)).ToList();
                return Fraction.ApproximateRational(t.Sum(v => v.Value)) / h;
                //return t.Sum() / h;
            });
        }
    }
}
