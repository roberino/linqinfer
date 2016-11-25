using LinqInfer.Learning.Classification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Maths.Probability
{
    public static class P
    {
        /// <summary>
        /// Creates a Markov Chain of a specified order
        /// by analysing the sequence of values
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="data">The sequence of data</param>
        /// <param name="delimiter">A delimiting function</param>
        /// <param name="order">The order to which the sequence will be analysed</param>
        /// <returns>a <see cref="IDiscreteMarkovChain{T}"/></returns>
        public static IDiscreteMarkovChain<T> AsMarkovChain<T>(this IEnumerable<T> data, Func<T, bool> delimiter = null, byte order = 1)
            where T : IEquatable<T>
        {
            var mkc = new DiscreteMarkovChain<T>(order);

            if (delimiter != null)
                mkc.AnalyseSequences(data, delimiter);
            else
                mkc.AnalyseSequence(data);

            return mkc;
        }

        /// <summary>
        /// Creates a time based histogram of items organised into bins of a specified width
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="data">A queryable set of data</param>
        /// <param name="timeFunction">A function to select the time period of an item</param>
        /// <param name="resolution">The size of each bin</param>
        /// <returns>A <see cref="DiscretizedSet{V, W, T}"/></returns>
        public static DiscretizedSet<DateTime, TimeSpan, T> AsHistogram<T>(this IQueryable<T> data, Expression<Func<T, DateTime>> timeFunction, TimeResolution resolution = TimeResolution.Hours)
        {
            if (resolution == TimeResolution.None) throw new ArgumentException("Invalid time resolution");

            var histogram = new Histogram((double)resolution);

            var results = histogram.Analyse(data, timeFunction);

            return results;
        }

        /// <summary>
        /// Creates a time based histogram of items organised into bins of a specified width
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="data">A queryable set of data</param>
        /// <param name="timeFunction">A function to select the time period of an item</param>
        /// <param name="resolution">The size of each bin</param>
        /// <returns>A <see cref="DiscretizedSet{V, W, T}"/></returns>
        public static DiscretizedSet<DateTime, TimeSpan, T> AsHistogram<T>(this IQueryable<T> data, Expression<Func<T, DateTime>> timeFunction, TimeSpan resolution)
        {
            if (resolution.Ticks == 0) throw new ArgumentException("Invalid time resolution");

            var histogram = new Histogram((double)resolution.TotalMilliseconds);

            var results = histogram.Analyse(data, timeFunction);

            return results;
        }

        /// <summary>
        /// Creates a histogram of items organised into bins of a specified width
        /// </summary>
        /// <typeparam name="T">The type of item</typeparam>
        /// <param name="data">A queryable set of data</param>
        /// <param name="valueFunction">A function to select the value of an item</param>
        /// <param name="width">The width of a bin</param>
        /// <returns>A <see cref="DiscretizedSet{V, W, T}"/></returns>
        public static DiscretizedSet<double, double, T> AsHistogram<T>(this IQueryable<T> data, Expression<Func<T, double>> valueFunction, double? width = null)
        {
            var histogram = new Histogram(width);

            var results = histogram.Analyse(data, valueFunction);

            return results;
        }

        /// <summary>
        /// Returns the product of a sequence of fraction values.
        /// </summary>
        public static Fraction Product(this IEnumerable<Fraction> values)
        {
            if (values.Any())
            {
                var t = Fraction.One;

                foreach(var value in values)
                {
                    t = Fraction.Multiply(t, value, true);
                }
            }

            return Fraction.Zero;
        }

        /// <summary>
        /// Converts a classification result set into a set of hypothetical outcomes.
        /// </summary>
        /// <typeparam name="T">The class / outcome type</typeparam>
        /// <param name="classifyResults">A set of classification results</param>
        /// <returns>A set of hypotheses</returns>
        public static Hypothetheses<T> ToHypotheses<T>(this IEnumerable<ClassifyResult<T>> classifyResults)
        {
            var cr = classifyResults.ToList();
            var total = cr.Sum(m => m.Score);
            var hypos = cr.Select(r => new HypotheticalOutcome<T>(r.ClassType, Fraction.ApproximateRational(r.Score / total)));
            return new Hypothetheses<T>(hypos);
        }

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
