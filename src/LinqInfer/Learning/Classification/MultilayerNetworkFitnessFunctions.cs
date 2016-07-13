using LinqInfer.Learning.Features;
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
        /// Returns a score which represents how accurately a classifier evaluates a set of pre-classified test examples.
        /// </summary>
        public static double ClassificationAccuracyScore<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, params ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>[] testData)
        {
            return ClassificationAccuranyScore(classifier.Classify, testData);
        }

        /// <summary>
        /// Returns a fitness function which can score a training context based on how accurately a classifier evaluates a set of pre-classified test examples.
        /// </summary>
        public static Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<TClass, NetworkParameters>, double> ClassificationAccuracyFunction<TInput, TClass>(params ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>[] testData)
        {
            return (f, c) =>
            {
                return ClassificationAccuranyScore(x => c.Classifier.Classify(f.ExtractVector(x)), testData);
            };
        }

        /// <summary>
        /// Returns a percentage representing the number of items successfully classified by a classifier.
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, params ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>[] testData)
        {
            var count = 0;

            if (testData.Length == 0) return 0d;

            foreach (var item in testData)
            {
                var classifyResults = classifier.Classify(item.ObjectInstance).ToList();

                var match = classifyResults.FirstOrDefault();
                count += (match == null ? 0 : (match.ClassType.Equals(item.Classification) ? 1 : 0));
            }

            return count / (double)testData.Length;
        }

        private static double ClassificationAccuranyScore<TInput, TClass>(this Func<TInput, IEnumerable<ClassifyResult<TClass>>> classifier, params ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>[] testData)
        {
            var cumulativeResult = 1d;

            foreach (var item in testData)
            {
                var classifyResults = classifier(item.ObjectInstance).ToList();

                if (classifyResults.Any())
                {
                    var match = classifyResults.FirstOrDefault(r => r.ClassType.Equals(item.Classification));
                    var total = classifyResults.Sum(r => r.Score);
                    var score = match == null ? 0 : match.Score;

                    cumulativeResult *= (score / total);
                }
                else
                {
                    cumulativeResult = 0;
                }
            }

            return cumulativeResult;
        }
    }
}
