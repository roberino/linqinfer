using System;
using System.Linq;

namespace LinqInfer.Math
{
    public interface IDensityEstimationStrategy<T>
    {
        Func<T, Fraction> Evaluate(IQueryable<T> sample);
    }
}
