using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkAsyncSink<TInput, TClass>
        : IBuilderSink<TrainingPair<IVector, IVector>, IVectorClassifier>
        where TInput : class where TClass : IEquatable<TClass>
    {
        private readonly IClassifierTrainer _trainingContext;
        private readonly Func<int, double, bool> _haltingFunction;

        public MultilayerNetworkAsyncSink(NetworkParameters parameters, Func<int, double, bool> haltingFunction)
        {
            var factory = new MultilayerNetworkTrainingContextFactory<TClass>();

            _trainingContext = factory.Create(parameters);
            _haltingFunction = haltingFunction;
        }

        public MultilayerNetworkAsyncSink(IClassifierTrainer trainer, Func<int, double, bool> haltingFunction)
        {
            var factory = new MultilayerNetworkTrainingContextFactory<TClass>();

            _trainingContext = trainer;
            _haltingFunction = haltingFunction;
        }

        public IVectorClassifier Classifier => _trainingContext.Output;

        public bool CanReceive => true;

        public IVectorClassifier Output => _trainingContext.Output;

        public Task ReceiveAsync(IBatch<TrainingPair<IVector, IVector>> dataBatch, CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(() =>
            {
                _trainingContext.Train(dataBatch.Items, (n, e) =>
                {
                    return _haltingFunction(n, e) || cancellationToken.IsCancellationRequested;
                });
            });
        }
    }
}