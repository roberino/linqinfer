using LinqInfer.Data;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning.Features;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    public static class AsyncPipelineExtensions
    {
        /// <summary>
        /// Creates an asyncronous pipeline from a data loading function
        /// </summary>
        /// <typeparam name="TInput"></typeparam>
        /// <param name="batchLoaderFunc">A batch loader function</param>
        /// <param name="featureExtractor">An optional feature extractor</param>
        public static IAsyncFeatureProcessingPipeline<TInput>
            CreatePipeline<TInput>(
                this Func<int, AsyncBatch<TInput>> batchLoaderFunc,
                IFloatingPointFeatureExtractor<TInput> featureExtractor = null)
            where TInput : class
        {
            var asyncEnum = new AsyncEnumerable<TInput>(batchLoaderFunc);
            var asyncEnumerator = new AsyncEnumerator<TInput>(asyncEnum);
            var pipeline = new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, featureExtractor);

            return pipeline;
        }

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
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            var outputMapper = new OutputMapperFactory<TInput, TClass>().Create(outputs);

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

        /// <summary>
        /// Published the data to messaging infrastructure
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The class type</typeparam>
        /// <param name="trainingSet">The training set</param>
        /// <param name="publisher">The publisher</param>
        /// <returns>The training set</returns>
        public static async Task<IAsyncTrainingSet<TInput, TClass>> SendAsync<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet,
            IMessagePublisher publisher,
            CancellationToken cancellationToken)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            await trainingSet
                .ExtractInputOutputIVectorBatches()
               .ProcessUsing(async b =>
               {
                   var batch = new TrainingBatch(b);
                   var msg = batch.AsMessage();

                   await publisher.PublishAsync(msg);
               }, cancellationToken);

            return trainingSet;
        }
    }
}