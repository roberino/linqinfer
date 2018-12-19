using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

        public static PipeOutput<ObjectVectorPair<TInput>, IVectorClassifier> AttachLongShortTermMemoryNetwork<TInput>(this IAsyncFeatureProcessingPipeline<TInput> pipeline)
        {
            var builder = RecurrentNetworkBuilder.Create(pipeline.FeatureExtractor.VectorSize);

            var trainingContext = builder
                .ConfigureLongShortTermMemoryNetwork(pipeline.FeatureExtractor.VectorSize)
                .Build();

            var sink = new RecurrentNetworkSink<TInput>(trainingContext);

            // new MultilayerNetworkObjectClassifier<TInput, TInput>(pipeline.FeatureExtractor, )

            return pipeline
                .ExtractBatches()
                .CreatePipe()
                .Attach(sink);
        }
    }

    class RecurrentNetworkSink<T> : IBuilderSink<ObjectVectorPair<T>, IVectorClassifier>
    {
        readonly IClassifierTrainingContext<INetworkModel> _trainingContext;

        public RecurrentNetworkSink(IClassifierTrainingContext<INetworkModel> trainingContext)
        {
            _trainingContext = trainingContext;
        }

        public IVectorClassifier Output => _trainingContext.Output;

        public bool CanReceive => true;

        public Task ReceiveAsync(IBatch<ObjectVectorPair<T>> dataBatch, CancellationToken cancellationToken)
        {
            if (dataBatch.Items.Count == 0)
            {
                return Task.CompletedTask;
            }

            var last = dataBatch.Items[0].Vector;

            _trainingContext
                .Train(dataBatch.Items.Skip(1).Select(x =>
                {
                    var next = last;
                    last = x.Vector;
                    return new TrainingPair<IVector, IVector>(next, x.Vector);
                }), (i, e) => cancellationToken.IsCancellationRequested);

            return Task.CompletedTask;
        }
    }
}