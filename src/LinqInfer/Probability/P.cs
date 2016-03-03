using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Probability
{
    public static class P
    {
        /// <summary>
        /// Builds a hypothesis for an outcome.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="outcome">The outcome (event)</param>
        /// <returns>A hypothesis builder</returns>
        public static HypothesisBuilder<T> Of<T>(T outcome)
        {
            return new HypothesisBuilder<T>(outcome);
        }

        /// <summary>
        /// Creates a fraction from a numerator and denominator.
        /// </summary>
        /// <param name="numerator">The numerator</param>
        /// <param name="denominator">The denominator</param>
        /// <returns>A fraction struct</returns>
        public static Fraction OutOf(this int numerator, int denominator)
        {
            return new Fraction(numerator, denominator);
        }

        /// <summary>
        /// Returns a hypotheses instance from an enumeration of hypothetical outcomes.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="hypos">The list of hypotheses</param>
        /// <returns>A Hypotheses object containing all outcomes</returns>
        public static Hypothetheses<T> AsHypotheses<T>(this IEnumerable<IHypotheticalOutcome<T>> hypos)
        {
            return new Hypothetheses<T>(hypos);
        }

        /// <summary>
        /// Returns a hypotheses instance from an enumeration of hypothetical outcomes.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="hypos">The list of hypotheses</param>
        /// <returns>A Hypotheses object containing all outcomes</returns>
        public static Hypothetheses<T> Hypotheses<T>(params IHypotheticalOutcome<T>[] hypos)
        {
            return new Hypothetheses<T>(hypos);
        }

        /// <summary>
        /// Converts a list of fractions into hypotheses. The order
        /// is used to create the outcome. E.g. first fraction = outcome 1.
        /// </summary>
        /// <param name="hypos">A list of prior probabilities</param>
        /// <returns>A Hypotheses object containing all outcomes</returns>
        public static Hypothetheses<int> Hypotheses(params Fraction[] hypos)
        {
            int n = 0;
            return new Hypothetheses<int>(hypos.Select(h => new HypotheticalOutcome<int>(n++, h)).ToList());
        }

        /// <summary>
        /// Creates a hypothesis from an outcome and prior probability.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="outcome">The outcome</param>
        /// <param name="priorProbability">The prior probability</param>
        /// <returns>A hypothetical outcome instance</returns>
        public static IHypotheticalOutcome<T> Hypothesis<T>(T outcome, Fraction priorProbability)
        {
            return new HypotheticalOutcome<T>(outcome, priorProbability);
        }

        /// <summary>
        /// Converts a dictionary of hypothetical outcomes and relevant frequencies
        /// into a set of hypotheses.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="outcomes">The outcomes and frequencies</param>
        /// <returns>A hypotheses containing the set of outcomes</returns>
        public static Hypothetheses<T> AsHypotheses<T>(this IDictionary<T, int> outcomes)
        {
            var total = outcomes.Sum(h => h.Value);
            var hypos = outcomes.Select(h => new HypotheticalOutcome<T>(h.Key, new Fraction(h.Value, total)));

            return Hypotheses(hypos.ToArray());
        }

        /// <summary>
        /// Converts a dictionary of hypothetical outcomes and relevant prior probabilities
        /// into a set of hypotheses.
        /// </summary>
        /// <typeparam name="T">The outcome type</typeparam>
        /// <param name="outcomes">The outcomes and priors</param>
        /// <returns>A hypotheses containing the set of outcomes</returns>
        public static Hypothetheses<T> AsHypotheses<T>(this IDictionary<T, Fraction> outcomes)
        {
            var hypos = outcomes.Select(h => new HypotheticalOutcome<T>(h.Key, h.Value));

            return Hypotheses(hypos.ToArray());
        }

        /// <summary>
        /// Returns a queryable sample of data
        /// from which frequency based probabilities
        /// can be calculated.
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <param name="sampleSpace">The data sample</param>
        /// <returns>A queryable sample</returns>
        public static IQueryableSample<T> AsSampleSpace<T>(this IQueryable<T> sampleSpace)
        {
            return new QueryableSample<T>(sampleSpace);
        }
    }
}
