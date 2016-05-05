using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    [DebuggerDisplay("{Name} Probability = {CurrentProbability}")]
    internal class HypothesisSubset<T> : QueryableSample<T>, IHypotheticalSubset<T>
    {
        private readonly Fraction _originalPrior;
        private readonly IQueryableSample<T> _superset;

        private Fraction _currentPrior;

        public HypothesisSubset(IQueryableSample<T> superset, IQueryable<T> sample, string name = null, Fraction? priorProbability = null, Expression<Func<T, int>> weightSelector = null) : base(sample, name, weightSelector)
        {
            _superset = superset;
            _originalPrior = priorProbability ?? new Fraction(Total(), superset.Total());
            _currentPrior = _originalPrior;
        }

        public IHypotheticalSubset<T> Update(Expression<Func<T, bool>> newEvidence)
        {
            var likelyhood = ProbabilityOfEvent(newEvidence);
            var normalisingFactor = _superset.ProbabilityOfEvent(newEvidence);

            _currentPrior = Output((_currentPrior * likelyhood) / normalisingFactor);

            return this;
        }

        public Fraction PosteriorProbability
        {
            get
            {
                return _currentPrior;
            }
        }

        public Fraction PriorProbability
        {
            get
            {
                return _originalPrior;
            }
        }
    }
}
