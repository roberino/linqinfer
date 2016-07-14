using LinqInfer.Learning.Features;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.IO;

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
            var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, extractor);

            classifierPipe.Train(trainingData, classf);

            return x => classifierPipe.Classify(x).ToDistribution();
        }
    }
}