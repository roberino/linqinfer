using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class FluentNetworkBuilder : IFluentNetworkBuilder
    {
        readonly IList<Func<LearningParameters, NetworkModuleSpecification>> _layers;
        readonly LearningParameters _learningParams;
        readonly int _inputVectorSize;
        readonly int _outputVectorSize;

        int _currentId;

        NetworkOutputSpecification _output;
        Action<NetworkLayerSpecification> _layerAction;

        internal FluentNetworkBuilder(int inputVectorSize, int outputVectorSize)
        {
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));

            _currentId = 0;
            _outputVectorSize = outputVectorSize;
            _learningParams = new LearningParameters();
            _layers = new List<Func<LearningParameters, NetworkModuleSpecification>>();
        }

        internal IFluentNetworkBuilder ConfigureLayers(Action<NetworkLayerSpecification> layerAction)
        {
            _layerAction = layerAction;

            return this;
        }

        public int CreateId() => ++_currentId;

        public IFluentNetworkBuilder ConfigureLearningParameters(Action<LearningParameters> config)
        {
            var lp = _learningParams.Clone(true);

            config(lp);

            lp.Validate();

            config(_learningParams);

            return this;
        }

        public IFluentNetworkBuilder ConfigureModule(Action<ModuleBuilderFactory> moduleConfig)
        {
            moduleConfig(new ModuleBuilderFactory(this));

            return this;
        }

        public IFluentNetworkBuilder AddModule(NetworkModuleSpecification networkModule)
        {
            _layers.Add(_ => networkModule);
            return this;
        }

        public IFluentNetworkBuilder AddHiddenLayer(int? layerSize = null,
            ActivatorExpression activator = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null,
            bool parallelProcess = false)
        {

            var layer = new NetworkLayerSpecification(
                CreateId(),
                layerSize.GetValueOrDefault(_inputVectorSize), activator,
                weightUpdateRule, initialWeightRange, parallelProcess);

            _layers.Add(p => layer);

            return this;
        }

        public IFluentNetworkBuilder ConfigureOutput(
            NetworkModuleSpecification outputModule,
            ILossFunction lossFunction,
            Func<int, ISerialisableDataTransformation> transformationFactory = null)
        {
            _output = new NetworkOutputSpecification(outputModule.Id, _outputVectorSize, lossFunction)
            {
                OutputTransformation = transformationFactory?.Invoke(_outputVectorSize)
            };

            return this;
        }

        public IFluentNetworkBuilder ConfigureOutput(
            ILossFunction lossFunction,
            Func<int, ISerialisableDataTransformation> transformationFactory = null)
        {
            _output = new NetworkOutputSpecification(-1, _outputVectorSize, lossFunction)
            {
                OutputTransformation = transformationFactory?.Invoke(_outputVectorSize)
            };

            return this;
        }

        public IClassifierTrainingContext<INetworkModel> Build()
        {
            var builtLayers = _layers.Select(f => f(_learningParams)).ToList();

            if (_layerAction != null)
            {
                foreach (var layer in builtLayers
                    .Where(l => l is NetworkLayerSpecification)
                    .Cast<NetworkLayerSpecification>())
                {
                    _layerAction(layer);
                }
            }

            if (builtLayers.Count > 0)
            {
                var last = builtLayers[0];

                foreach (var next in builtLayers.Skip(1))
                {
                    if (!last.Connections.AreDefined)
                    {
                        last.ConnectTo(next);
                    }

                    last = next;
                }
            }

            if (_output == null)
            {
                ConfigureOutput(builtLayers.Last(), LossFunctions.Square);
            }
            else
            {
                if (_output.OutputModuleId == -1)
                {
                    ConfigureOutput(builtLayers.Last(), _output.LossFunction, _ => _output.OutputTransformation);
                }
            }

            var spec = new NetworkSpecification(
                _learningParams,
                _inputVectorSize,
                _output,
                builtLayers.ToArray());

            return new MultilayerNetworkTrainingContext(new MultilayerNetwork(spec));
        }
    }
}