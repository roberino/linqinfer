using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkTrainingContextFactory<TClass> where TClass : IEquatable<TClass>
    {
        private int _currentId;

        public MultilayerNetworkTrainingContextFactory()
        {
            _currentId = 0;
        }

        public IClassifierTrainingContext<NetworkParameters> Create(MultilayerNetwork network)
        {
            return new MlnTrainingContext(() => ++_currentId, network);
        }

        public IClassifierTrainingContext<NetworkParameters> Create(NetworkParameters parameters)
        {
            return new MlnTrainingContext(() => ++_currentId, parameters);
        }

        [DebuggerDisplay("{AverageError}:{Parameters}")]
        private class MlnTrainingContext : IClassifierTrainingContext<NetworkParameters>
        {
            private readonly MultilayerNetwork _network;
            private readonly IAssistedLearningProcessor _rawLearningProcessor;
            private readonly Func<int> _idFunc;

            private double? _lastError;
            private double? _error;
            private int _trainingCounter;

            public MlnTrainingContext(Func<int> idFunc, NetworkParameters parameters)
            {
                _network = new MultilayerNetwork(parameters);

                var bpa = new BackPropagationLearning(_network);

                _rawLearningProcessor = bpa;
                _idFunc = idFunc;

                Id = idFunc();
                Parameters = parameters;
            }

            public MlnTrainingContext(Func<int> idFunc, MultilayerNetwork network)
            {
                _network = network;

                var bpa = new BackPropagationLearning(_network);
                
                _idFunc = idFunc;

                Id = idFunc();
                Parameters = network.Parameters;
            }

            public int Id { get; private set; }

            public int IterationCounter { get; set; }

            public IVectorClassifier Output { get { return _network; } }

            public NetworkParameters Parameters { get; private set; }

            public double? CumulativeError { get { return _error; } }

            public double? AverageError { get { return _error.HasValue && _trainingCounter > 0 ? _error / _trainingCounter : null; } }

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

            public void PruneInputs(params int[] inputIndexes)
            {
                _network.PruneInputs(inputIndexes);
            }

            public double Train(IVector outputVector, IVector sampleVector)
            {
                if (!_error.HasValue) _error = 0;

                var err = _rawLearningProcessor.Train(sampleVector, outputVector);

                _error += err;

                _trainingCounter++;

                return err;
            }

            public double Train(IEnumerable<TrainingPair<IVector, IVector>> trainingData, Func<int, double, bool> haltingFunction)
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
                return string.Format("{0}: (iter {1}) => err = {2}, params = {3}", Id, IterationCounter, AverageError, Parameters);
            }

            public IClassifierTrainingContext<NetworkParameters> Clone(bool deep)
            {
                return new MlnTrainingContext(_idFunc, _network.Clone(true))
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
                _network.Specification.LearningParameters.LearningRate = rateAdjustment(_network.Specification.LearningParameters.LearningRate);
            }
        }
    }
}