using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    sealed class ConvolutionalNetworkBuilder : INetworkBuilder, IConvolutionalNetworkBuilder
    {
        readonly int? _expectedOutputSize;
        readonly IList<NetworkModuleSpecification> _layers;
        readonly TrainingParameters trainingParams;
        readonly int _inputVectorSize;

        int _currentId;

        NetworkOutputSpecification _output;

        ConvolutionalNetworkBuilder(int inputVectorSize, int? expectedOutputSize = null)
        {
            _expectedOutputSize = expectedOutputSize;
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));
            
            _currentId = 0;
            trainingParams = new TrainingParameters();
            _layers = new List<NetworkModuleSpecification>();
        }

        int CreateId() => ++_currentId;

        public static IConvolutionalNetworkBuilder Create(int inputVectorSize, int? expectedOutputSize = null)
        {
            return new ConvolutionalNetworkBuilder(inputVectorSize, expectedOutputSize);
        }

        public IConvolutionalNetworkBuilder ConfigureLearningParameters(Action<TrainingParameters> config)
        {
            var lp = trainingParams.Clone(true);

            config(lp);

            lp.Validate();

            config(trainingParams);

            return this;
        }

        public IConvolutionalNetworkBuilder AddHiddenLayer(int layerSize,
            ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null)
        {
            var layer = new NetworkLayerSpecification(
                CreateId(),
                layerSize, activator,
                weightUpdateRule, initialWeightRange);
            
            _layers.LastOrDefault()?.ConnectTo(layer);

            _layers.Add(layer);

            return this;
        }

        public INetworkBuilder ConfigureOutput(
            ILossFunction lossFunction,
            Func<int, ISerialisableDataTransformation> transformationFactory = null,
            ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null)
        {
            AddHiddenLayer(_expectedOutputSize.GetValueOrDefault(_inputVectorSize), activator ?? Activators.None(), weightUpdateRule, initialWeightRange);
            
            var outputLayer = (NetworkLayerSpecification)_layers.Last();

            _output = new NetworkOutputSpecification(outputLayer, lossFunction)
            {
                OutputTransformation = transformationFactory?.Invoke(outputLayer.LayerSize)
            };

            return this;
        }

        public INetworkBuilder ApplyDefaults()
        {
            if (_output == null)
            {
                return ConfigureOutput(LossFunctions.Square);
            }

            return this;
        }

        public IClassifierTrainingContext<INetworkModel> Build()
        {
            var last = _layers[0];

            foreach (var next in _layers.Skip(1))
            {
                if (!last.Connections.AreDefined)
                {
                    last.ConnectTo(next);
                }

                last = next;
            }

            var spec = new NetworkSpecification(
                trainingParams,
                _inputVectorSize,
                _output,
                _layers.ToArray());

            return new MultilayerNetworkTrainingContext(new MultilayerNetwork(spec));
        }
    }
}