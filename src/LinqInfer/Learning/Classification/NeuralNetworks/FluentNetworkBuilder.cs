using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    static class FluentMultilayerNetworkBuilderExtensions
    {
        /// <summary>
        /// Builds a softmax network configuration
        /// with a single hidden layer
        /// </summary>
        public static IFluentNetworkBuilder ConfigureSoftmaxNetwork(this IFluentNetworkBuilder builder, int hiddenLayerSize, Action<LearningParameters> learningConfig = null)
        {
            return builder.ParallelProcess()
                .ConfigureLearningParameters(p =>
                {
                    learningConfig?.Invoke(p);
                })
                .AddHiddenLinearLayer(hiddenLayerSize, WeightUpdateRules.Default())
                .AddSoftmaxOutput();
        }

        public static IFluentNetworkBuilder AddHiddenSigmoidLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize)
        {
            if (layerSize == 0) return specificationBuilder;

            return specificationBuilder.
                AddHiddenLayer(layerSize, Activators.Sigmoid(1), LossFunctions.Square);
        }

        public static IFluentNetworkBuilder AddHiddenLinearLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize, WeightUpdateRule updateRule = null)
        {
            if (layerSize == 0) return specificationBuilder;

            updateRule = updateRule ?? WeightUpdateRules.Default();

            return specificationBuilder.
                AddHiddenLayer(
                        layerSize,
                        Activators.None(),
                        LossFunctions.Square,
                        updateRule,
                        NetworkLayerSpecification.DefaultInitialWeightRange);
        }

        public static IFluentNetworkBuilder AddSoftmaxOutput(this IFluentNetworkBuilder specificationBuilder)
        {
            return specificationBuilder
                .ConfigureOutputLayer(Activators.None(), LossFunctions.CrossEntropy, null, WeightUpdateRules.Default())
                .TransformOutput(x => new Softmax(x));
        }

        public static IFluentNetworkBuilder ParallelProcess(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).ConfigureLayers(l => l.ParallelProcess = true);
        }

        public static IClassifierTrainingContext<INetworkModel> Build(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).Build();
        }
    }

    public sealed class ModuleFactory
    {
        readonly FluentNetworkBuilder _networkBuilder;

        internal ModuleFactory(FluentNetworkBuilder networkBuilder)
        {
            _networkBuilder = networkBuilder;
        }

        public NetworkModuleSpecification Module(VectorAggregationType aggregationType)
        {
            var module = new NetworkModuleSpecification(_networkBuilder.CreateId())
            {
                InputOperator = aggregationType
            };

            _networkBuilder.AddModule(module);

            return module;
        }

        public NetworkLayerSpecification Layer(int layerSize, ActivatorExpression activator)
        {
            var layerSpec = new NetworkLayerSpecification(_networkBuilder.CreateId(), layerSize, activator);

            _networkBuilder.AddModule(layerSpec);

            return layerSpec;
        }

        public void Output(NetworkModuleSpecification moduleSpecification)
        {
            _networkBuilder.SetOutput(moduleSpecification);
        }
    }

    public sealed class FluentNetworkBuilder : IFluentNetworkBuilder
    {
        readonly IList<Func<LearningParameters, NetworkModuleSpecification>> _layers;
        readonly LearningParameters _learningParams;
        readonly int _inputVectorSize;

        int _currentId;

        NetworkLayerSpecification _output;
        NetworkModuleSpecification _specifiedOutput;
        Action<NetworkLayerSpecification> _layerAction;

        internal FluentNetworkBuilder(int inputVectorSize, int outputVectorSize)
        {
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));

            _currentId = 0;
            _learningParams = new LearningParameters();
            _layers = new List<Func<LearningParameters, NetworkModuleSpecification>>();
            _output = new NetworkLayerSpecification(_currentId, outputVectorSize, Activators.Sigmoid(), LossFunctions.Square, WeightUpdateRules.Default(), NetworkLayerSpecification.DefaultInitialWeightRange);
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

        public IFluentNetworkBuilder ConfigureModule(Action<ModuleFactory> moduleConfig)
        {
            moduleConfig(new ModuleFactory(this));

            return this;
        }

        public IFluentNetworkBuilder AddModule(NetworkModuleSpecification networkModule)
        {
            _layers.Add(_ => networkModule);
            return this;
        }

        public IFluentNetworkBuilder AddHiddenLayer(int? layerSize = null,
            ActivatorExpression activator = null,
            ILossFunction lossFunction = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null,
            bool parallelProcess = false)
        {

            var layer = new NetworkLayerSpecification(
                CreateId(),
                layerSize.GetValueOrDefault(_inputVectorSize), activator,
                lossFunction, weightUpdateRule, initialWeightRange, parallelProcess);

            _layers.Add(p => layer);

            return this;
        }

        public void SetOutput(NetworkModuleSpecification moduleSpecification)
        {
            _specifiedOutput = moduleSpecification;
        }

        public IFluentNetworkBuilder ConfigureOutputLayer(
            ActivatorExpression activator,
            ILossFunction lossFunction,
            Range? initialWeightRange = null,
            WeightUpdateRule updateRule = null)
        {
            var tx = _output.OutputTransformation;

            updateRule = updateRule ?? WeightUpdateRules.Default();

            _currentId++;

            _output = new NetworkLayerSpecification(
                _currentId,
                _output.LayerSize,
                activator,
                lossFunction,
                updateRule,
                initialWeightRange.GetValueOrDefault(_output.InitialWeightRange))
            {
                OutputTransformation = tx
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(ISerialisableDataTransformation transformation)
        {
            _output = new NetworkLayerSpecification(_output.Id, _output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformation
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory)
        {
            _output = new NetworkLayerSpecification(_output.Id, _output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformationFactory(_output.LayerSize)
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
                _layerAction(_output);
            }

            var outMod = _specifiedOutput ?? _output;

            if (builtLayers.Count > 0)
            {
                var last = builtLayers[0];

                foreach (var next in builtLayers.Skip(1))
                {
                    if (!last.Connections.AreDefined && last.Id != outMod.Id)
                    {
                        last.ConnectTo(next);
                    }

                    last = next;
                }
            }

            if (builtLayers.All(m => m.Id != outMod.Id))
            {
                builtLayers.Add(outMod);
            }

            var spec = new NetworkSpecification(
                _learningParams,
                _inputVectorSize,
                builtLayers.ToArray());

            spec.SetOutput(outMod, _output.LayerSize);

            return new MultilayerNetworkTrainingContext(new MultilayerNetwork(spec));
        }
    }
}