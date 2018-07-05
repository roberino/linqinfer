using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using System;

namespace LinqInfer.Learning
{
    public static class MlnExtensions
    {
        /// <summary>
        /// Attaches a classifier to the output of the training pipeline
        /// using previously created classifier data
        /// </summary>
        public static INetworkClassifier<TClass, TInput> AttachMultilayerNetworkClassifier<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet,
            PortableDataDocument existingClassifierData) where TInput : class where TClass : IEquatable<TClass>
        {
            var targetDoc = existingClassifierData.FindChild<MultilayerNetwork>() ?? existingClassifierData;
            var network = MultilayerNetwork.CreateFromData(targetDoc);
            var trainingContext = new MultilayerNetworkTrainingContext<NetworkSpecification>(() => 1, network, network.Specification);
            var sink = new MultilayerNetworkAsyncSink<TClass>(trainingContext, trainingContext.Parameters.LearningParameters);
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, (MultilayerNetwork)sink.Output);

            trainingSet.RegisterSinks(sink);

            return classifier;
        }

        /// <summary>
        /// Creates a multi-layer neural network classifier, training the network using the supplied feature data.
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="trainingSet">A asyncronous training set</param>
        /// <param name="networkBuilder">A delegate which builds the network specification</param>
        public static INetworkClassifier<TClass, TInput> AttachMultilayerNetworkClassifier<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet,
            Action<FluentNetworkBuilder> networkBuilder) where TInput : class where TClass : IEquatable<TClass>
        {
            var builder = new FluentNetworkBuilder(trainingSet.FeaturePipeline.FeatureExtractor.VectorSize, trainingSet.OutputMapper.VectorSize);

            networkBuilder?.Invoke(builder);

            var trainingContext = builder.Build();

            var sink = new MultilayerNetworkAsyncSink<TClass>(trainingContext, trainingContext.Parameters.LearningParameters);
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, (MultilayerNetwork)sink.Output);

            trainingSet.RegisterSinks(sink);

            return classifier;
        }
    }
}
