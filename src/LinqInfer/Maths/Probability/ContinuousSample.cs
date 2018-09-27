using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    class ContinuousSample<T>
    {
        Func<T, Fraction> _kde;

        public ContinuousSample(IQueryable<T> sample, IDensityEstimationStrategy<T> estimationModel)
        {
            Contract.Assert(estimationModel != null);
            _kde = estimationModel.Evaluate(sample);
        }

        public Fraction DensityOf(T item)
        {
            return _kde(item);
        }
    }
}
