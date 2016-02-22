using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Probability
{
    internal class HypoSet<T> : Sample<SampleBase<T>>
    {
        public HypoSet(string name = null) : base(name) { }

        public Fraction ProbabilityOfEvent(Expression<Func<T, bool>> eventPredicate)
        {
            var total = _sampleSpace.Sum(h => h.Total());
            var sumOfE = _sampleSpace.Sum(h => h.Count(eventPredicate));

            return new Fraction(sumOfE, total);
        }

        public Fraction PosterierProbability(SampleBase<T> hypo, Expression<Func<T, bool>> newEvent)
        {
            if (!_sampleSpace.Contains(hypo))
            {
                return new Fraction(0, 0);
            }

            var prior = new Fraction(1, _sampleSpace.Count);
            var likelyhood = hypo.ProbabilityOfEvent(newEvent);
            var normalisingFactor = ProbabilityOfEvent(newEvent);

            return Output((prior * likelyhood) / normalisingFactor);
        }
    }
}
