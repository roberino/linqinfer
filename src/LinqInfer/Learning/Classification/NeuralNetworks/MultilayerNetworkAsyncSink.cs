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
    internal class MultilayerNetworkAsyncSink<TClass>
        : IBuilderSink<TrainingPair<IVector, IVector>, IVectorClassifier>
        where TClass : IEquatable<TClass>
    {
        private readonly IClassifierTrainer _trainingContext;
        private readonly LearningParameters _learningParameters;

        private readonly ValueStore _errorHistory;
        
        public MultilayerNetworkAsyncSink(
            IClassifierTrainer trainer,
            LearningParameters learningParameters)
        {
            _trainingContext = trainer;
            _learningParameters = learningParameters;
            _errorHistory = new ValueStore(learningParameters.ErrorHistoryCount);

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
                _errorHistory.Register(_trainingContext.AverageError.Value);

                if (_errorHistory.Trend > 0)
                {
                    DebugOutput.Log("Error increasing");
                }

                var status = new TrainingStatus
                {
                    AverageError = _trainingContext.AverageError.Value,
                    Iteration = _errorHistory.Count,
                    Trend = _errorHistory.Trend,
                    MovingError =  _errorHistory.MovingError
                };

                if (_learningParameters.EvaluateHaltingFunction(status))
                {
                    CanReceive = false;

                    DebugOutput.Log("Halted due to halt condition");
                }
            }
        }
    }
}