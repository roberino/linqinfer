using System;
using System.Linq;

namespace LinqInfer.Probability
{
    public interface IDensityEstimationStrategy<T>
    {
        Func<T, Fraction> Evaluate(IQueryable<T> sample);
    }
}
