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
    }
}
