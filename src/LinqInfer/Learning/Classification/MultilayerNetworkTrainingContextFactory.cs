using LinqInfer.Learning.Features;
using System;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkTrainingContextFactory<TClass> where TClass : IEquatable<TClass>
    {
        private readonly ICategoricalOutputMapper<TClass> _outputMapper;

        public MultilayerNetworkTrainingContextFactory(ICategoricalOutputMapper<TClass> outputMapper)
        {
            _outputMapper = outputMapper;
        }

        public IClassifierTrainingContext<TClass, NetworkParameters> Create(NetworkParameters parameters)
        {
            return new MlnTrainngContext(parameters, _outputMapper);
        }

        private class MlnTrainngContext : IClassifierTrainingContext<TClass, NetworkParameters>
        {
            private readonly AssistedLearningAdapter<TClass> _learningAdapter;
            private readonly BackPropagationLearning _bpa;
            private double? _error;
            private int _trainingCounter;

            public MlnTrainngContext(NetworkParameters parameters, ICategoricalOutputMapper<TClass> outputMapper)
            {
                var network = new MultilayerNetwork(parameters);

                _bpa = new BackPropagationLearning(network);
                _learningAdapter = new AssistedLearningAdapter<TClass>(_bpa, outputMapper);

                Parameters = parameters;
                Classifier = new MultilayerNetworkClassifier<TClass>(outputMapper, network);
            }

            public IFloatingPointClassifier<TClass> Classifier { get; private set; }

            public NetworkParameters Parameters { get; private set; }

            public double? CumulativeError { get { return _error; } }

            public double? AverageError { get { return _error.HasValue ? _error / _trainingCounter : null; } }

            public void ResetError()
            {
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

            public double Train(TClass sampleClass, ColumnVector1D sample)
            {
                if (!_error.HasValue) _error = 0;

                var err = _learningAdapter.Train(sampleClass, sample);

                _error += err;

                _trainingCounter++;

                return err;
            }
        }
    }
}