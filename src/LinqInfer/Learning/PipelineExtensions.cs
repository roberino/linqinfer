using LinqInfer.Data;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning
{
    public static class PipelineExtensions
    {
        public static FeaturePipline<T> CreatePipeline<T>(this IQueryable<T> data) where T : class
        {
            return new FeaturePipline<T>(data);
        }

        public static ExecutionPipline<FeatureMap<TInput>> ToSofm<TInput>(this FeaturePipline<TInput> pipeline, TInput normalisingSample = null, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var fm = new FeatureMapper<TInput>(p.FeatureExtractor, normalisingSample, outputNodeCount, learningRate);

                return fm.Map(p.Data);
            });
        }

        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToNaiveBayesClassifier<TInput, TClass>(this FeaturePipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf) where TInput : class
        {
            return pipeline.ProcessWith((p, n) =>
            {
                var net = new NaiveBayesNormalClassifier<TClass>(p.FeatureExtractor.VectorSize);
                var classifierPipe = new ClassificationPipeline<TClass, TInput, double>(net, net, p.FeatureExtractor);

                classifierPipe.Train(pipeline.Data, classf);

                return (IObjectClassifier<TClass, TInput>)classifierPipe;
            });
        }
        
        public static ExecutionPipline<IObjectClassifier<TClass, TInput>> ToMultilayerNetworkClassifier<TInput, TClass>(this FeaturePipline<TInput> pipeline, Expression<Func<TInput, TClass>> classf, float errorTolerance = 0.1f) where TInput : class where TClass : IEquatable<TClass>
        {
            var classifierPipe = new MultilayerNetworkClassificationPipeline<TClass, TInput>(pipeline.FeatureExtractor, errorTolerance);

            return pipeline.ProcessWith((p, n) =>
            {
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
        public static IObjectClassifier<TClass, TInput> OpenAsMultilayerNetworkClassifier<TInput, TClass>(
            this IBlobStore store, string key, IFloatingPointFeatureExtractor<TInput> featureExtractor) where TInput : class where TClass : IEquatable<TClass>
        {
            var classifierPipe = new MultilayerNetworkClassificationPipeline<TClass, TInput>(featureExtractor);

            store.Restore(key, classifierPipe);

            return classifierPipe;
        }
    }
}