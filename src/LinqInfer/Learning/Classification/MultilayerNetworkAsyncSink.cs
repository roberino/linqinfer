using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    class MultilayerNetworkAsyncSink<TInput, TClass>
        : IAsyncSink<TrainingPair<IVector, IVector>>
        where TInput : class where TClass : IEquatable<TClass>
    {
        private readonly IClassifierTrainingContext<NetworkParameters> _trainingContext;
        private readonly Func<int, double, bool> _haltingFunction;

        public MultilayerNetworkAsyncSink(NetworkParameters parameters, Func<int, double, bool> haltingFunction)
        {
            var factory = new MultilayerNetworkTrainingContextFactory<TClass>();

            _trainingContext = factory.Create(parameters);
            _haltingFunction = haltingFunction;
        }

        public IVectorClassifier Classifier => _trainingContext.Output;

        public bool CanReceive => true;

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