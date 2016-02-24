using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Probability
{
    public class Hypothetheses<T>
    {
        private readonly IDictionary<T, IHypotheticalOutcome<T>> _hypos;

        internal Hypothetheses(IEnumerable<IHypotheticalOutcome<T>> hypos)
        {
            _hypos = hypos.ToDictionary(h => h.Outcome);
        }

        public IEnumerable<IHypotheticalOutcome<T>> Hypotheses { get { return _hypos.Values; } }

        public IHypotheticalOutcome<T> this[T item] { get { return _hypos[item]; } }

        public Fraction ProbabilityOf(T item)
        {
            return this[item].PosteriorProbability;
        }

        public IDictionary<T, IDictionary<V, double>> DistributionOver<V>(Func<T, V, Fraction> likelyhoodFunc, IEnumerable<V> values)
        {
            var dist = Hypotheses.ToDictionary(h => h.Outcome, h => (IDictionary<V, double>)values.ToDictionary(v => v, v => 0d));

            foreach (var v in values)
            {
                var posteriors = CalculateDistribution(h => likelyhoodFunc(h, v));

                foreach(var p in posteriors)
                {
                    dist[p.Key][v] = p.Value.Value;
                }
            }

            return dist;
        }

        public IDictionary<T, double> CurrentDistribution()
        {
            return Hypotheses.ToDictionary(h => h.Outcome, h => h.PosteriorProbability.Value);
        }

        public void Update(Func<T, Fraction> likelyhoodFunc)
        {
            Update(Hypotheses.Select(h => h.Outcome).Select(likelyhoodFunc).ToArray());
        }

        public void Update(params Fraction[] newEvents)
        {
            int i = 0;

            var nf = Hypotheses.Select(h => newEvents[i++] * h.PriorProbability).ToList().Sum();

            i = 0;

            foreach (var h in Hypotheses)
            {
                h.Update(newEvents[i++], nf);
            }
        }

        private IDictionary<T, Fraction> CalculateDistribution(Func<T, Fraction> likelyhoodFunc)
        {
            return CalculateDistribution(Hypotheses.Select(h => h.Outcome).Select(likelyhoodFunc).ToArray());
        }

        private IDictionary<T, Fraction> CalculateDistribution(params Fraction[] newEvents)
        {
            int i = 0;

            var nf = Hypotheses.Select(h => newEvents[i++] * h.PriorProbability).ToList().Sum();

            i = 0;

            return Hypotheses
                .Select(h => new { h = h, p = h.Calculate(newEvents[i++], nf) })
                .ToDictionary(x => x.h.Outcome, x => x.p);
        }
    }
}
