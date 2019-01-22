using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using System;

namespace LinqInfer.Learning
{
    public static class LstmExtensions
    {
        public static INetworkClassifier<TClass, TInput> AttachLongShortTermMemoryNetwork<TInput, TClass>(
            this IAsyncTrainingSet<TInput, TClass> trainingSet)
            where TClass : IEquatable<TClass>
        {
            var factory = NetworkFactory<TInput>.CreateNetworkFactory(trainingSet.FeaturePipeline.FeatureExtractor);

            var context = factory.BuildLongShortTermMemoryNetwork(trainingSet.OutputMapper);

            var sink = new MultilayerNetworkAsyncSink<TClass>(context.trainer, context.trainer.Model.Specification.LearningParameters);
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, (MultilayerNetwork)sink.Output);

            trainingSet.RegisterSinks(sink);

            return classifier;
        }
    }
}