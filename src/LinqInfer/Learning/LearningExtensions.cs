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
        public static IDictionary<TClass, Fraction> ToDistribution<TClass>(this IEnumerable<ClassifyResult<TClass>> classifyResults)
        {
            var cr = classifyResults.ToList();
            var total = cr.Sum(m => m.Score);
            return cr.ToDictionary(m => m.ClassType, m => Fraction.ApproximateRational(m.Score / total));
        }

        /// <summary>
        /// Creates a function which, based on training data
        /// can classify a new object type and return a distribution
        /// of potential class matches.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <returns>A function which can classify new objects, returning a dictionary of potential results</returns>
        public static Func<TInput, IDictionary<TClass, Fraction>> ToSimpleDistributionFunction<TInput, TClass>(this IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            var extractor = new ObjectFeatureExtractor().CreateFeatureExtractor<TInput>();
            var net = new NaiveBayesNormalClassifier<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return x => classifierPipe.Classify(x).ToDistribution();
        }
    }
}