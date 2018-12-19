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
            var builder = RecurrentNetworkBuilder
                .Create(trainingSet.FeaturePipeline.FeatureExtractor.VectorSize)
                .ConfigureLongShortTermMemoryNetwork(trainingSet.OutputMapper.VectorSize);

            var trainingContext = builder.Build();

            var sink = new MultilayerNetworkAsyncSink<TClass>(trainingContext, trainingContext.Model.Specification.LearningParameters);
            var classifier = new MultilayerNetworkObjectClassifier<TClass, TInput>(trainingSet.FeaturePipeline.FeatureExtractor, trainingSet.OutputMapper, (MultilayerNetwork)sink.Output);

            trainingSet.RegisterSinks(sink);

            return classifier;
        }
    }
}