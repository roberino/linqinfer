using LinqInfer.Learning.Features;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class LearningExtensions
    {
        private static readonly ObjectFeatureExtractor _ofo = new ObjectFeatureExtractor();

        /// <summary>
        /// Creates a self-organising feature map
        /// from an enumeration of objects.
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <param name="values">The values</param>
        /// <param name="normalisingSample">A sample which is used to normalise the data (provide an object which represents the maximum value for each parameter)</param>
        /// <param name="outputNodeCount">The maximum number of cluster nodes to output</param>
        /// <param name="normaliseData">True if the object vector should be normalised before processing</param>
        /// <param name="learningRate">The rate of learning</param>
        /// <returns>An enumeration of cluster nodes</returns>
        public static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, T normalisingSample = null, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where T : class
        {
            var fm = new FeatureMapper<T>(_ofo.CreateFeatureExtractor<T>(normaliseData), normalisingSample, outputNodeCount, learningRate);

            return fm.Map(values);
        }

        internal static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, IFloatingPointFeatureExtractor<T> featureExtractor, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var fm = new FeatureMapper<T>(featureExtractor, default(T), outputNodeCount, learningRate);

            return fm.Map(values);
        }

        internal static FeatureMap<T> ToSofm<T>(this IQueryable<T> values, Func<T, float[]> featureExtractorFunc, string[] featureLabels = null, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(featureExtractorFunc, featureExtractorFunc(default(T)).Length, false, featureLabels);

            var fm = new FeatureMapper<T>(featureExtractor, default(T), outputNodeCount, learningRate);

            return fm.Map(values);
        }

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
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new NaiveBayesNormalClassifier<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return x => classifierPipe.Classify(x).ToDistribution();
        }

        /// <summary>
        /// Creates a simple classifier based on some training data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <returns>A function which can classify new objects, returning the best match</returns>
        public static Func<TInput, IEnumerable<ClassifyResult<TClass>>> ToNaiveBayesClassifier<TInput, TClass>(this IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new NaiveBayesNormalClassifier<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return classifierPipe.Classify;
        }

        /// <summary>
        /// Creates a multi-layer network classifier based on some training data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <returns>A function which can classify new objects, returning the best match</returns>
        public static Func<TInput, IEnumerable<ClassifyResult<TClass>>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf, float errorTolerance = 0.1f) where TInput : class where TClass : IEquatable<TClass>
        {
            Contract.Assert(trainingData != null && classf != null && errorTolerance > 0);

            var extractor = _ofo.CreateDoublePrecisionFeatureExtractor<TInput>();
            var classifierPipe = new MultilayerNetworkClassificationPipeline<TClass, TInput>(extractor, errorTolerance);

            classifierPipe.Train(trainingData, classf);

            return classifierPipe.Classify;
        }

        /// <summary>
        /// Creates a multi-layer network classifier based on some training data. A solution will
        /// continue to be sought until the goal function is satisfied.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="trainingData">The training data set</param>
        /// <param name="classf">A function which will be used to classify the training data</param>
        /// <param name="goalFunction">A function which returns true when the classifier is acceptable</param>
        /// <returns>A function which can classify new objects, returning the best match</returns>
        public static Func<TInput, IEnumerable<ClassifyResult<TClass>>> ToMultilayerNetworkClassifier<TInput, TClass>(
            this IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classf, 
            Func<Func<TInput, ClassifyResult<TClass>>, bool> goalFunction, float errorTolerance = 0.1f) where TInput : class where TClass : IEquatable<TClass>
        {
            Contract.Assert(trainingData != null && classf != null && goalFunction != null && errorTolerance > 0);

            foreach (var n in Enumerable.Range(1, 250))
            {
                var cls = ToMultilayerNetworkClassifier(trainingData, classf, errorTolerance);

                try
                {
                    if (goalFunction(x => cls(x).FirstOrDefault())) return cls;
                }
                catch (NullReferenceException)
                {
                }
            }

            throw new ArgumentException("Solution not found");
        }
    }
}