using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    public static class AsyncPipelineExtensions
    {
        /// <summary>
        /// Using principal component analysis, reduces the least significant features
        /// keeping a specified number of features (dimensions)
        /// </summary>
        /// <param name="numberOfDimensions">The (max) number of features to retain</param>
        /// <param name="sampleSize">The size of the sample to use for analysis</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public static async Task<IAsyncFeatureProcessingPipeline<T>> PrincipalComponentReductionAsync<T>(this IAsyncFeatureProcessingPipeline<T> asyncFeatureProcessingPipeline, int numberOfDimensions, int sampleSize = 100)
            where T : class
        {
            var samples = await asyncFeatureProcessingPipeline.ExtractBatches().ToMemoryAsync(CancellationToken.None, sampleSize);

            var pca = new PrincipalComponentAnalysis(samples.Select(s => s.Vector));

            var pp = pca.CreatePrincipalComponentTransformer(numberOfDimensions, sampleSize);

            return asyncFeatureProcessingPipeline.PreprocessWith(pp);
        }

        /// <summary>
        /// Using principal component analysis, reduces the least significant features
        /// keeping a specified number of features (dimensions)
        /// </summary>
        /// <param name="pipelineTask">A pipeline task</param>
        /// <param name="numberOfDimensions">The (max) number of features to retain</param>
        /// <param name="sampleSize">The size of the sample to use for analysis</param>
        /// <returns>The feature processing pipeline with the transform applied</returns>
        public static async Task<IAsyncFeatureProcessingPipeline<T>> PrincipalComponentReductionAsync<T>(
            this Task<IAsyncFeatureProcessingPipeline<T>> pipelineTask, int numberOfDimensions,
            int sampleSize = 100)
            where T : class
        {
            var pipeline = await pipelineTask;

            return await pipeline.PrincipalComponentReductionAsync(numberOfDimensions, sampleSize);
        }

        /// <summary>
        /// Centres and scales the data
        /// </summary>
        /// <typeparam name="T">The input type</typeparam>
        /// <param name="pipelineTask">A pipeline task</param>
        /// <param name="range">The scale range</param>
        /// <returns>A pipeline with a centre and scale transform</returns>
        public static async Task<IAsyncFeatureProcessingPipeline<T>> CentreAndScaleAsync<T>(
            this Task<IAsyncFeatureProcessingPipeline<T>> pipelineTask, Maths.Range? range = null)
        {
            var pipeline = await pipelineTask;

            return await pipeline.CentreAndScaleAsync(range);
        }

        /// <summary>
        /// Builds an asynchronous pipeline, given a number of feature extractor strategies
        /// </summary>
        public static async Task<IAsyncFeatureProcessingPipeline<TInput>> BuildPipelineAsync<TInput>(
            this ITransformingAsyncBatchSource<TInput> asyncEnumerator,
            CancellationToken cancellationToken,
            params IFeatureExtractionStrategy<TInput>[] strategies)
            where TInput : class
        {
            var builder = new FeatureExtractorBuilder<TInput>(typeof(TInput), strategies);

            var fe = await builder.BuildAsync(asyncEnumerator, cancellationToken);

            return new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, fe);
        }

        /// <summary>
        /// Creates an asynchronous pipeline, given a feature extractor
        /// </summary>
        public static IAsyncFeatureProcessingPipeline<TInput> CreatePipeline<TInput>(
            this IEnumerable<TInput> dataset,
            IVectorFeatureExtractor<TInput> featureExtractor)
        {
            return new AsyncFeatureProcessingPipeline<TInput>(dataset.AsAsyncEnumerator(), featureExtractor);
        }

        /// <summary>
        /// Creates a time sequence which can be used for training
        /// </summary>
        public static async Task<IAsyncTrainingSet<TInput, TInput>> CreateCategoricalTimeSequenceTrainingSetAsync<TInput>(
                this ITransformingAsyncBatchSource<TInput> dataset,
                int sampleSize = 10000)
            where TInput : IEquatable<TInput>
        {
            var categories = await dataset.ToDistinctSetAsync(CancellationToken.None, sampleSize);

            var cfe = new CategoricalFeatureExtractor<TInput, TInput>(x => x, categories);
            return new TimeSequenceAsyncTrainingSet<TInput>(dataset, cfe);
        }

        /// <summary>
        /// Creates a time sequence which can be used for training
        /// </summary>
        public static IAsyncTrainingSet<TInput, TInput> CreateCategoricalTimeSequenceTrainingSet<TInput>(
                this IAsyncFeatureProcessingPipeline<TInput> dataset,
                IOneHotEncoding<TInput> encoding)
            where TInput : IEquatable<TInput>
        {
            var outputMapper = new OutputMapper<TInput>(encoding);
            return new TimeSequenceAsyncTrainingSet<TInput>(dataset, outputMapper);
        }

        /// <summary>
        /// Creates an asynchronous pipeline, given a feature extractor
        /// </summary>
        public static IAsyncFeatureProcessingPipeline<TInput> CreatePipeline<TInput>(
            this IEnumerable<TInput> dataset,
            Expression<Func<TInput, IVector>> featureSelectExpression, int inputVectorSize)
        {
            var fe = new ExpressionFeatureExtractor<TInput>(featureSelectExpression, inputVectorSize);
            return CreatePipeline(dataset, fe);
        }

        /// <summary>
        /// Creates an asynchronous pipeline, given a feature extractor
        /// </summary>
        internal static IAsyncFeatureProcessingPipeline<TInput> CreatePipeline<TInput>(
            this ITransformingAsyncBatchSource<TInput> asyncEnumerator,
            IVectorFeatureExtractor<TInput> featureExtractor)
            where TInput : class
        {
            return new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, featureExtractor);
        }

        /// <summary>
        /// Creates an asynchronous pipeline, given a feature extractor function
        /// </summary>
        public static IAsyncFeatureProcessingPipeline<TInput> CreatePipeline<TInput>(
            this ITransformingAsyncBatchSource<TInput> asyncEnumerator,
            Expression<Func<TInput, IVector>> featureExtractorFunction,
            int vectorSize)
            where TInput : class
        {
            var featureExtractor = new ExpressionFeatureExtractor<TInput>(featureExtractorFunction, vectorSize);
            return new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, featureExtractor);
        }

        /// <summary>
        /// Creates an asynchronous pipeline from a data loading function
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="batchLoaderFunc">A batch loader function</param>
        /// <param name="featureExtractor">An optional feature extractor</param>
        public static IAsyncFeatureProcessingPipeline<TInput>
            CreatePipeline<TInput>(
                this Func<int, AsyncBatch<TInput>> batchLoaderFunc,
                IVectorFeatureExtractor<TInput> featureExtractor = null)
            where TInput : class
        {
            var asyncEnum = new AsyncBatchEnumerable<TInput>(batchLoaderFunc);
            var asyncEnumerator = new AsyncEnumerator<TInput>(asyncEnum);
            var pipeline = new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, featureExtractor ?? new ObjectFeatureExtractor<TInput>());

            return pipeline;
        }

        /// <summary>
        /// Creates a training set from an async feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="classf">A classifying expression</param>
        /// <param name="outputs">Outputs</param>
        /// <returns>A training set</returns>
        public static IAsyncTrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(
            this IAsyncFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            params TClass[] outputs)
            where TClass : IEquatable<TClass>
        {
            var omf = new OutputMapperFactory<TInput, TClass>();
            var outputMapper = omf.Create(outputs);

            return new AsyncTrainingSet<TInput, TClass>(pipeline, classf, outputMapper);
        }


        /// <summary>
        /// Creates a training set from an async feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipelineTask">A feature pipeline task</param>
        /// <param name="classf">A classifying expression (which will be used to find outputs by sampling the data)</param>
        /// <param name="cancellationToken">A cancellation token to cancel the process</param>
        /// <returns>A training set</returns>
        public static async Task<IAsyncTrainingSet<TInput, TClass>> AsTrainingSetAsync<TInput, TClass>(
            this Task<IAsyncFeatureProcessingPipeline<TInput>> pipelineTask,
            Expression<Func<TInput, TClass>> classf,
            CancellationToken cancellationToken)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var pipeline = await pipelineTask;
            var omf = new OutputMapperFactory<TInput, TClass>();
            var outputMapper = await omf.CreateAsync(pipeline, classf, cancellationToken);

            return new AsyncTrainingSet<TInput, TClass>(pipeline, classf, outputMapper);
        }

        /// <summary>
        /// Creates a training set from an async feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="classf">A classifying expression</param>
        /// <param name="outputMapper">An output mapper</param>
        /// <returns>A training set</returns>
        public static IAsyncTrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(
            this IAsyncFeatureProcessingPipeline<TInput> pipeline,
            Expression<Func<TInput, TClass>> classf,
            ICategoricalOutputMapper<TClass> outputMapper)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            return new AsyncTrainingSet<TInput, TClass>(pipeline, classf, outputMapper);
        }
    }
}