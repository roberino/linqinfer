using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public static class ClassifyResultExtensions
    {
        /// <summary>
        /// Converts a classification result set into a set of hypothetical outcomes.
        /// </summary>
        /// <typeparam name="T">The class / outcome type</typeparam>
        /// <param name="classifyResults">A set of classification results</param>
        /// <returns>A set of hypotheses</returns>
        public static Hypothetical<T> ToHypothetical<T>(this IEnumerable<ClassifyResult<T>> classifyResults)
        {
            var cr = classifyResults.ToList();
            var total = cr.Sum(m => m.Score);
            var hypos = cr.Select(r => new HypotheticalOutcome<T>(r.ClassType, Fraction.ApproximateRational(r.Score / total)));
            return new Hypothetical<T>(hypos);
        }
    }
}