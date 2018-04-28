using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkAsyncSink<TInput, TClass>
        : IBuilderSink<TrainingPair<IVector, IVector>, IVectorClassifier>
        where TInput : class where TClass : IEquatable<TClass>
    {
        private readonly IClassifierTrainer _trainingContext;
        private readonly Func<int, double, bool> _haltingFunction;

        private double? _lastError;
        
        public MultilayerNetworkAsyncSink(IClassifierTrainer trainer, Func<int, double, bool> haltingFunction)
        {
            _trainingContext = trainer;
            _haltingFunction = haltingFunction;

            Reset();
        }

        public IVectorClassifier Classifier => _trainingContext.Output;

        public bool CanReceive { get; private set; }

        public IVectorClassifier Output => _trainingContext.Output;

        public void Reset()
        {
            CanReceive = true;
        }

        public async Task ReceiveAsync(IBatch<TrainingPair<IVector, IVector>> dataBatch, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                _trainingContext.Train(dataBatch.Items, (n, e) => cancellationToken.IsCancellationRequested);

                DebugOutput.Log($"Average error: {_trainingContext.AverageError}");
            });

            if (_trainingContext.AverageError.HasValue)
            {
                if (_lastError.HasValue)
                {
                    var rateOrErr = (_trainingContext.AverageError - _lastError);

                    if (rateOrErr > 0)
                    {
                        // CanReceive = false;

                        DebugOutput.Log("Error increasing");
                    }
                }

                _lastError = _trainingContext.AverageError;

                if (_haltingFunction(dataBatch.BatchNumber, _trainingContext.AverageError.Value))
                {
                    CanReceive = false;

                    DebugOutput.Log("Halted due to halt condition");
                }
            }
        }
    }
}