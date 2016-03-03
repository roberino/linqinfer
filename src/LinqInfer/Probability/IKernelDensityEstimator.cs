using System;
using System.Linq;

namespace LinqInfer.Probability
{
    public interface IKernelDensityEstimator<T>
    {
        Func<T, Fraction> Evaluate(IQueryable<T> sample, float bandwidth = 0.2f);
    }
}
