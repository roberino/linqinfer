using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class LearningExtensions
    {
        /// <summary>
        /// Converts the results of a classifier into a distribution of probabilities by class type.
        /// </summary>
        /// <typeparam name="TClass">The class type</typeparam>
        /// <param name="classifyResults">The classification results</param>
        /// <returns>A dictionary of TClass / Fraction pairs</returns>
        public static IDictionary<TClass, Fraction> ToDistribution<TClass>(
            this IEnumerable<ClassifyResult<TClass>> classifyResults)
        {
            var cr = classifyResults.ToList();
            var total = cr.Sum(m => m.Score);
            return cr.ToDictionary(m => m.ClassType, m => Fraction.ApproximateRational(m.Score / total));
        }
    }
}