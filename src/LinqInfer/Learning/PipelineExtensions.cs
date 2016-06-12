using LinqInfer.Data;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class PipelineExtensions
    {
        /// <summary>
        /// Returns an object for transforming and working with features.
        /// </summary>
        /// <typeparam name="T">The input type</typeparam>
        /// <param name="featureExtractor">An optional feature extractor</param>
        /// <returns></returns>
        public static IFeatureTransformBuilder<T> CreateFeatureTransformation<T>(IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipline<T>(Enumerable.Empty<T>().AsQueryable(), featureExtractor);
        }

        /// <summary>
        /// Creates a feature processing pipeline from a set of data.
        /// </summary>
        /// <typeparam name="T">The input data type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="featureExtractor">An optional feature extractor to extract feature vectors from the data</param>
        /// <returns></returns>
        public static FeatureProcessingPipline<T> CreatePipeline<T>(this IQueryable<T> data, IFloatingPointFeatureExtractor<T> featureExtractor = null) where T : class
        {
            return new FeatureProcessingPipline<T>(data, featureExtractor);
        }

        /// <summary>
        /// Creates a self-organising feature map using the supplied feature data. Items will be clustered based on Euclidean distance.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="normalisingSample">A sample which will be used to normalise the data (can be null if the feature extractor supports it)</param>
        /// <param name="outputNodeCount">The maximum number of output nodes</param>
        /// <param name="normaliseData">True if the data should be normalised prior to mapping</param>
        /// <param name="learningRate">The learning rate</param>
        /// <returns></returns>
        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeatureProcessingPipline<TInput> pipeline, TInput normalisingSample = null, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapper<TInput>(p.FeatureExtractor, normalisingSample, outputNodeCount, learningRate);

                return fm.Map(p.Data);
            });
        }

        /// <summary>
        /// Creates a basic Naive Bayesian classifiers, training the classifier using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <returns></returns>
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this FeatureProcessingPipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var net = new NaiveBayesNormalClassifier<TClass>(p.FeatureExtractor.VectorSize);
                var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, p.FeatureExtractor);

                classifierPipe.Train(pipeline.Data, classf);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }

        /// <summary>
        /// Creates a multi-layer neural network, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A pipeline of feature data</param>
        /// <param name="classf">An expression to teach the classifier the class of an individual item of data</param>
        /// <param name="errorTolerance">The network error tolerance</param>
        /// <returns></returns>
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(this FeatureProcessingPipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf, float errorTolerance = 0.1f) where TInput : class where TClass : IEquatable<TClass>
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var classifierPipe = new MultilayerNetworkClassificationPipeline<TClass, TInput>(pipeline.FeatureExtractor, errorTolerance);

                classifierPipe.Train(pipeline.Data, classf);

                if (n != null) pipeline.OutputResults(classifierPipe, n);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }

        /// <summary>
        /// Restores a previously saved multi-layer network classifier from a blob store.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The returned class type</typeparam>
        /// <param name="store">A blob store</returns>
        /// <param name="key">The name of a previously stored classifier</returns>
        public static IObjectClassifier<TClass, TInput> OpenMultilayerNetworkClassifier<TInput, TClass>(
            this IBlobStore store, string key, IFloatingPointFeatureExtractor<TInput> featureExtractor = null) where TInput : class where TClass : IEquatable<TClass>
        {
            if (featureExtractor == null) featureExtractor = new FeatureProcessingPipline<TInput>().FeatureExtractor;

            var classifierPipe = new MultilayerNetworkClassificationPipeline<TClass, TInput>(featureExtractor);

            store.Restore(key, classifierPipe);

            return classifierPipe;
        }
    }
}