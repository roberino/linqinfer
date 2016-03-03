using System;
using System.Linq;

namespace LinqInfer.Probability
{
    class KernelDensityEstimator : IKernelDensityEstimator<Fraction>
    {
        private readonly Func<Fraction, Fraction> _k;

        public KernelDensityEstimator(Func<Fraction, Fraction> kernelFunc)
        {
            _k = kernelFunc;
        }

        public Func<Fraction, Fraction> Evaluate(IQueryable<Fraction> sample, float bandwidth = 0.2F)
        {
            // h = bandwidth
            // Sum(K(y - x[i]) / h) [over sample data]


            throw new NotImplementedException();
        }
    }
}
