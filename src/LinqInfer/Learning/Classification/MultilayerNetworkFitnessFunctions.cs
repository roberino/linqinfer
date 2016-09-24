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
        /// Returns a percentage representing the number of items successfully classified by a classifier
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, ITrainingSet<TInput, TClass> trainingSet)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var testSet = trainingSet.FeaturePipeline.Data.ClassifyUsing(trainingSet.ClassifyingExpression.Compile());

            return ClassificationAccuracyPercentageInternal(classifier, testSet);
        }

        /// <summary>
        /// Returns a percentage representing the number of items successfully classified by a classifier
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, IEnumerable<TInput> testData, Func<TInput, TClass> classf)
        {
            var testSet = testData.ClassifyUsing(classf);
            
            return ClassificationAccuracyPercentageInternal(classifier, testSet);
        }

        /// <summary>
        /// Returns a percentage representing the number of items successfully classified by a classifier
        /// </summary>
        public static double ClassificationAccuracyPercentage<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, params ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>[] testData)
        {
            return ClassificationAccuracyPercentageInternal(classifier, testData);
        }

        private static double ClassificationAccuracyPercentageInternal<TInput, TClass>(this IObjectClassifier<TClass, TInput> classifier, IEnumerable<ClassifiedObjectExtensions.ClassifiedObject<TInput, TClass>> testData)
        {
            var count = 0;

            if (!testData.Any()) return 0d;

            foreach (var item in testData)
            {
                var classifyResults = classifier.Classify(item.ObjectInstance).ToList();

                var match = classifyResults.FirstOrDefault();
                count += (match == null ? 0 : (match.ClassType.Equals(item.Classification) ? 1 : 0));
            }

            return count / (double)testData.Count();
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
