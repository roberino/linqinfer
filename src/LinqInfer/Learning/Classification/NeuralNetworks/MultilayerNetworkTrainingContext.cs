using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    [DebuggerDisplay("{AverageError}:{Parameters}")]
    class MultilayerNetworkTrainingContext<TParams> : IClassifierTrainingContext<TParams>
    {
        readonly MultilayerNetwork _network;
        readonly IAssistedLearningProcessor _rawLearningProcessor;
        readonly Func<int> _idFunc;

        double? _lastError;
        double? _error;
        int _trainingCounter;

        public MultilayerNetworkTrainingContext(Func<int> idFunc, MultilayerNetwork network, TParams parameters)
        {
            _network = network;

            _rawLearningProcessor = new BackPropagationLearning(_network);

            _idFunc = idFunc;

            Id = idFunc();
            Parameters = parameters;
        }

        public int Id { get; }

        public int IterationCounter { get; set; }

        public IVectorClassifier Output => _network;

        public TParams Parameters { get; }

        public double? CumulativeError => _error;

        public double? AverageError => _error.HasValue && _trainingCounter > 0 ? _error / _trainingCounter : null;

        public double? RateOfErrorChange
        {
            get
            {
                if (_error.HasValue && _lastError.HasValue)
                {
                    return (_lastError - _error) / _lastError;
                }

                return null;
            }
        }

        public void ResetError()
        {
            _lastError = _error;
            _trainingCounter = 0;
            _error = null;
        }

        public double Train(IVector sampleVector, IVector outputVector)
        {
            if (!_error.HasValue) _error = 0;

            var err = _rawLearningProcessor.Train(sampleVector, outputVector);

            _error += err;

            _trainingCounter++;

            return err;
        }

        public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData,
            Func<int, double, bool> haltingFunction)
        {
            if (!_error.HasValue) _error = 0;

            var err = _rawLearningProcessor.Train(trainingData, (n, e) =>
            {
                _trainingCounter++;
                return haltingFunction(n, e);
            });

            _error += err;

            return err;
        }

        public override string ToString()
        {
            return string.Format("{0}: (iter {1}) => err = {2}, params = {3}", Id, IterationCounter, AverageError,
                Parameters);
        }

        public IClassifierTrainingContext<TParams> Clone(bool deep)
        {
            return new MultilayerNetworkTrainingContext<TParams>(_idFunc, _network.Clone(true), Parameters)
            {
                _error = _error,
                _lastError = _lastError,
                _trainingCounter = _trainingCounter
            };
        }

        public object Clone()
        {
            return Clone(true);
        }

        public void AdjustLearningRate(Func<double, double> rateAdjustment)
        {
            _network.Specification.LearningParameters.LearningRate =
                rateAdjustment(_network.Specification.LearningParameters.LearningRate);
        }
    }
}