using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification
{
    public static class MultilayerNetworkFitnessFunctions
    {
        /// <summary>
        /// Returns the inverse error (1/e) accumulated over a training interation.
        /// </summary>
        public static Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<TClass, NetworkParameters>, double> ErrorMinimisationFunction<TInput, TClass>()
        {
            return ((f, c) => c.CumulativeError.HasValue ? 1d / c.CumulativeError.Value : 0);
        }

        /// <summary>
        /// Returns the highest classification returned and the relative score as a percent.
        /// </summary>
        public static Tuple<TClass, Fraction> HighestClassification<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, TInput input)
        {
            var results = classifier.Classify(input);
            var total = results.Sum(r => r.Score);
            var first = results.FirstOrDefault();

            if (first == null)
            {
                return null;
            }

            return new Tuple<TClass, Fraction>(first.ClassType, Fraction.ApproximateRational(first.Score / total));
        }

        /// <summary>
        /// Returns a percentage representing the number of items successfully classified by a classifier
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, ITrainingSet<TInput, TClass> trainingSet)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            return ClassificationAccuracyPercentageInternal(classifier, trainingSet.ExtractTrainingObjects());
        }

        /// <summary>
        /// Returns a percentage representing the number of items successfully classified by a classifier
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, params TrainingPair<TInput, TClass>[] testData)
        {
            return ClassificationAccuracyPercentageInternal(classifier, testData);
        }

        private static double ClassificationAccuracyPercentageInternal<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, IEnumerable<TrainingPair<TInput, TClass>> testData)
        {
            var count = 0;

            if (!testData.Any()) return 0d;

            foreach (var item in testData)
            {
                var classifyResults = classifier.Classify(item.Input).ToList();

                var match = classifyResults.FirstOrDefault();

                count += (match == null ? 0 : (match.ClassType.Equals(item.TargetOutput) ? 1 : 0));
            }

            return count / (double)testData.Count();
        }
    }
}