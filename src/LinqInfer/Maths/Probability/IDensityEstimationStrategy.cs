using System;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    public interface IDensityEstimationStrategy<T>
    {
        Func<T, Fraction> Evaluate(IQueryable<T> sample);
    }
}
