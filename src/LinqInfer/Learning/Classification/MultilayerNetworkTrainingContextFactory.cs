using LinqInfer.Learning.Features;
using System;
using LinqInfer.Maths;
using System.Diagnostics;
using LinqInfer.Data;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkTrainingContextFactory<TClass> where TClass : IEquatable<TClass>
    {
        private int _currentId;
        private readonly ICategoricalOutputMapper<TClass> _outputMapper;

        public MultilayerNetworkTrainingContextFactory(ICategoricalOutputMapper<TClass> outputMapper)
        {
            _outputMapper = outputMapper;
            _currentId = 0;
        }

        public IClassifierTrainingContext<TClass, NetworkParameters> Create(NetworkParameters parameters)
        {
            return new MlnTrainngContext(() => ++_currentId, parameters, _outputMapper);
        }

        [DebuggerDisplay("{AverageError}:{Parameters}")]
        private class MlnTrainngContext : IClassifierTrainingContext<TClass, NetworkParameters>
        {
            private readonly MultilayerNetwork _network;
            private readonly IAssistedLearningProcessor _rawLearningProcessor;
            private readonly AssistedLearningAdapter<TClass> _learningAdapter;
            private readonly MultilayerNetworkClassifier<TClass> _classifier;
            private readonly Func<int> _idFunc;

            private double? _lastError;
            private double? _error;
            private int _trainingCounter;

            public MlnTrainngContext(Func<int> idFunc, NetworkParameters parameters, ICategoricalOutputMapper<TClass> outputMapper)
            {
                _network = new MultilayerNetwork(parameters);

                var bpa = new BackPropagationLearning(_network);

                _rawLearningProcessor = bpa;
                _learningAdapter = new AssistedLearningAdapter<TClass>(bpa, outputMapper);
                _classifier = new MultilayerNetworkClassifier<TClass>(outputMapper, _network);

                _idFunc = idFunc;

                Id = idFunc();
                Parameters = parameters;
            }

            private MlnTrainngContext(Func<int> idFunc, MultilayerNetwork network, ICategoricalOutputMapper<TClass> outputMapper)
            {
                _network = network;

                var bpa = new BackPropagationLearning(_network);

                _learningAdapter = new AssistedLearningAdapter<TClass>(bpa, outputMapper);
                _classifier = new MultilayerNetworkClassifier<TClass>(outputMapper, _network);
                _idFunc = idFunc;

                Id = idFunc();
                Parameters = network.Parameters;
            }

            public int Id { get; private set; }

            public int IterationCounter { get; set; }

            public IBinaryPersistable Output { get { return _network; } }

            public IFloatingPointClassifier<TClass> Classifier { get { return _classifier; } }

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
                ((MultilayerNetworkClassifier<TClass>)Classifier).Network.PruneInputs(inputIndexes);
            }

            public double Train(TClass sampleClass, double[] sample)
            {
                return Train(sampleClass, new ColumnVector1D(sample));
            }

            public double Train(ColumnVector1D outputVector, ColumnVector1D sampleVector)
            {
                if (!_error.HasValue) _error = 0;

                var err = _rawLearningProcessor.Train(sampleVector, outputVector);

                _error += err;

                _trainingCounter++;

                return err;
            }

            public double Train(TClass sampleClass, ColumnVector1D sample)
            {
                if (!_error.HasValue) _error = 0;

                var err = _learningAdapter.Train(sampleClass, sample);

                _error += err;

                _trainingCounter++;

                return err;
            }

            public override string ToString()
            {
                return string.Format("{0}: (iter {1}) => err = {2}, params = {3}", Id, IterationCounter, AverageError, Parameters);
            }

            public IClassifierTrainingContext<TClass, NetworkParameters> Clone(bool deep)
            {
                return new MlnTrainngContext(_idFunc, _network.Clone(true), _classifier.OutputMapper)
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
        }
    }
}