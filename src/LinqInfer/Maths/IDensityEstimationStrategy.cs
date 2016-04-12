using System;
using System.Linq;

namespace LinqInfer.Maths
{
    public interface IDensityEstimationStrategy<T>
    {
        Func<T, Fraction> Evaluate(IQueryable<T> sample);
    }
}
