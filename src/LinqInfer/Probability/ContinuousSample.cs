using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Probability
{
    internal class ContinuousSample<T>
    {
        private Func<T, Fraction> _kde;

        public ContinuousSample(IQueryable<T> sample, IDensityEstimationStrategy<T> estimationModel)
        {
            Contract.Assert(estimationModel != null);
            _kde = estimationModel.Evaluate(sample);
        }

        public Fraction ProbabilityOf(T item)
        {
            //
            return _kde(item);
        }
    }
}
