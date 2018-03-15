﻿using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal static class FluentMultilayerNetworkBuilderExtensions
    {
        public static IFluentNetworkBuilder AddHiddenSigmoidLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize)
        {
            return specificationBuilder.
                AddHiddenLayer(new LayerSpecification(layerSize, Activators.Sigmoid(1), LossFunctions.Square));

        }

        public static IFluentNetworkBuilder AddSoftmaxOutput(this IFluentNetworkBuilder specificationBuilder)
        {
            return specificationBuilder
                .ConfigureOutputLayer(Activators.None(), LossFunctions.CrossEntropy)
                .TransformOutput(x => new Softmax(x));
        }

        public static IFluentNetworkBuilder ParallelProcess(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).ConfigureLayers(l => l.ParallelProcess = true);
        }

        public static IClassifierTrainingContext<NetworkSpecification> Build(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).Build();
        }
    }

    public sealed class FluentNetworkBuilder : IFluentNetworkBuilder
    {
        private readonly IList<LayerSpecification> _layers;
        private readonly Range _defaultWeightRange;
        private LearningParameters _learningParams;
        private LayerSpecification _output;
        private Action<LayerSpecification> _layerAction;
        private int _inputVectorSize;

        internal FluentNetworkBuilder(int inputVectorSize, int outputVectorSize)
        {
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));

            _defaultWeightRange = new Range(0.05, -0.05);
            _learningParams = new LearningParameters();
            _layers = new List<LayerSpecification>();
            _output = new LayerSpecification(outputVectorSize, Activators.Sigmoid(), LossFunctions.Square, DefaultWeightUpdateRule.Create(_learningParams.LearningRate), _defaultWeightRange);
        }

        internal IFluentNetworkBuilder ConfigureLayers(Action<LayerSpecification> layerAction)
        {
            _layerAction = layerAction;

            return this;
        }

        public IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config)
        {
            var lp = _learningParams.Clone(true);

            config(lp);

            lp.Validate();

            _learningParams = lp;

            return this;
        }

        public IFluentNetworkBuilder ConfigureLearningParameters(LearningParameters learningParameters)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));

            learningParameters.Validate();

            _learningParams = learningParameters;

            return this;
        }

        public IFluentNetworkBuilder AddHiddenLayer(LayerSpecification layer)
        {
            _layers.Add(layer);
            return this;
        }

        public IFluentNetworkBuilder ConfigureOutputLayer(IActivatorFunction activator, ILossFunction lossFunction, Range? initialWeightRange = null)
        {
            var tx = _output.OutputTransformation;

            _output = new LayerSpecification(_output.LayerSize, activator, lossFunction, DefaultWeightUpdateRule.Create(_learningParams.LearningRate), initialWeightRange.GetValueOrDefault(_output.InitialWeightRange))
            {
                OutputTransformation = tx
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(ISerialisableVectorTransformation transformation)
        {
            _output = new LayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformation
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableVectorTransformation> transformationFactory)
        {
            _output = new LayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformationFactory(_output.LayerSize)
            };

            return this;
        }

        public IClassifierTrainingContext<NetworkSpecification> Build()
        {
            if (_layerAction != null)
            {
                foreach (var layer in _layers)
                {
                    _layerAction(layer);
                }
                _layerAction(_output);
            }

            var spec = new NetworkSpecification(_learningParams,
                _inputVectorSize, _layers.Concat(new[] { _output }).ToArray());

            int id = 1;

            return new MlnCtx(() => Interlocked.Increment(ref id), spec);
        }

        private class MlnCtx : IClassifierTrainingContext<NetworkSpecification>
        {
            private readonly MultilayerNetwork _network;
            private readonly IAssistedLearningProcessor _rawLearningProcessor;
            private readonly Func<int> _idFunc;

            private double? _lastError;
            private double? _error;
            private int _trainingCounter;

            public MlnCtx(Func<int> idFunc, NetworkSpecification parameters)
            {
                _network = new MultilayerNetwork(parameters);

                var bpa = new BackPropagationLearning(_network);

                _rawLearningProcessor = bpa;
                _idFunc = idFunc;

                Id = idFunc();
                Parameters = parameters;
            }

            public MlnCtx(Func<int> idFunc, MultilayerNetwork network)
            {
                _network = network;

                var bpa = new BackPropagationLearning(_network);

                _idFunc = idFunc;

                Id = idFunc();
                Parameters = network.Specification;
            }

            public int Id { get; }

            public int IterationCounter { get; set; }

            public IVectorClassifier Output => _network;

            public NetworkSpecification Parameters { get; }

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

            public IClassifierTrainingContext<NetworkSpecification> Clone(bool deep)
            {
                return new MlnCtx(_idFunc, _network.Clone(true))
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