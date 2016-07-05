using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths.Probability
{
    /// <summary>
    /// Represents a set of hypotheses
    /// </summary>
    /// <typeparam name="T">The type of hypothesis</typeparam>
    public class Hypothetheses<T>
    {
        private readonly IDictionary<T, IHypotheticalOutcome<T>> _hypos;

        internal Hypothetheses(IEnumerable<IHypotheticalOutcome<T>> hypos)
        {
            _hypos = hypos.ToDictionary(h => h.Outcome);
        }

        /// <summary>
        /// Fires when the set is updated with new evidence
        /// </summary>
        public event EventHandler Updated;

        /// <summary>
        /// Returns the hypothetical outcomes
        /// </summary>
        public IEnumerable<IHypotheticalOutcome<T>> Hypotheses { get { return _hypos.Values; } }

        /// <summary>
        /// Returns a hypothetical outcome by outcome type
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IHypotheticalOutcome<T> this[T item] { get { return _hypos[item]; } }

        /// <summary>
        /// Returns the current posterior probability of an outcome
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public Fraction ProbabilityOf(T item)
        {
            return this[item].PosteriorProbability;
        }

        /// <summary>
        /// Returns the most probable outcome based on the posterior probability
        /// </summary>
        /// <returns></returns>
        public T MostProbable()
        {
            return _hypos
                .OrderByDescending(h => h.Value.PosteriorProbability.Value)
                .Select(h => h.Key)
                .FirstOrDefault();
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

        /// <summary>
        /// Returns the outcomes as a discrete distribution
        /// </summary>
        /// <returns></returns>
        public IDictionary<T, double> CurrentDistribution()
        {
            return Hypotheses.ToDictionary(h => h.Outcome, h => h.PosteriorProbability.Value);
        }

        /// <summary>
        /// Updates each hypothesis using a likelyhood function which takes each hypothetical outcome as a parameter and returns a likelyhood value
        /// </summary>
        /// <param name="likelyhoodFunc">The likelyhood funtion</param>
        public void Update(Func<T, Fraction> likelyhoodFunc)
        {
            Update(Hypotheses.Select(h => h.Outcome).Select(likelyhoodFunc).ToArray());
        }

        /// <summary>
        /// Updates each hypothesis using a new set of likelyhood values for each outcome
        /// </summary>
        /// <param name="newEvents"></param>
        public void Update(params Fraction[] newEvents)
        {
            int i = 0;

            var nf = Hypotheses.Select(h => newEvents[i++] * h.PriorProbability).ToList().Sum(true);

            i = 0;

            foreach (var h in Hypotheses)
            {
                h.Update(newEvents[i++], nf);
            }

            var ev = Updated;

            if (ev != null) ev.Invoke(this, EventArgs.Empty);
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
