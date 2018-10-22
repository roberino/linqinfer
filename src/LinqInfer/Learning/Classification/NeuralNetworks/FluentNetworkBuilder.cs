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
                AddHiddenLayer(new NetworkLayerSpecification(layerSize, Activators.Sigmoid(1), LossFunctions.Square));
        }

        public static IFluentNetworkBuilder AddHiddenLinearLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize, WeightUpdateRule updateRule = null)
        {
            if (layerSize == 0) return specificationBuilder;

            updateRule = updateRule ?? WeightUpdateRules.Default();

            return specificationBuilder.
                AddHiddenLayer(p => 
                    new NetworkLayerSpecification(
                        layerSize, 
                        Activators.None(), 
                        LossFunctions.Square,
                        updateRule,
                        NetworkLayerSpecification.DefaultInitialWeightRange));
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

        public static IClassifierTrainingContext<NetworkSpecification> Build(this IFluentNetworkBuilder specificationBuilder)
        {
            return ((FluentNetworkBuilder)specificationBuilder).Build();
        }
    }

    public sealed partial class FluentNetworkBuilder : IFluentNetworkBuilder
    {
        readonly IList<Func<LearningParameters, NetworkLayerSpecification>> _layers;
        readonly Range _defaultWeightRange;
        LearningParameters _learningParams;
        NetworkLayerSpecification _output;
        Action<NetworkLayerSpecification> _layerAction;
        int _inputVectorSize;

        internal FluentNetworkBuilder(int inputVectorSize, int outputVectorSize)
        {
            _inputVectorSize = ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            ArgAssert.AssertGreaterThanZero(outputVectorSize, nameof(outputVectorSize));

            _defaultWeightRange = new Range(0.05, -0.05);
            _learningParams = new LearningParameters();
            _layers = new List<Func<LearningParameters, NetworkLayerSpecification>>();
            _output = new NetworkLayerSpecification(outputVectorSize, Activators.Sigmoid(), LossFunctions.Square, WeightUpdateRules.Default(), _defaultWeightRange);
        }

        internal IFluentNetworkBuilder ConfigureLayers(Action<NetworkLayerSpecification> layerAction)
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

        public IFluentNetworkBuilder AddHiddenLayer(NetworkLayerSpecification networkLayer)
        {
            _layers.Add(_ => networkLayer);
            return this;
        }

        public IFluentNetworkBuilder AddHiddenLayer(Func<LearningParameters, NetworkLayerSpecification> layerFactory)
        {
            _layers.Add(layerFactory);
            return this;
        }

        public IFluentNetworkBuilder ConfigureOutputLayer(
            ActivatorExpression activator, 
            ILossFunction lossFunction, 
            Range? initialWeightRange = null, 
            WeightUpdateRule updateRule = null)
        {
            var tx = _output.OutputTransformation;
            
            updateRule = updateRule ??  WeightUpdateRules.Default();

            _output = new NetworkLayerSpecification(
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
            _output = new NetworkLayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformation
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory)
        {
            _output = new NetworkLayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformationFactory(_output.LayerSize)
            };

            return this;
        }

        public IClassifierTrainingContext<NetworkSpecification> Build()
        {
            var builtLayers = _layers.Select(f => f(_learningParams)).ToList();

            if (_layerAction != null)
            {
                foreach (var layer in builtLayers)
                {
                    _layerAction(layer);
                }
                _layerAction(_output);
            }

            var spec = new NetworkSpecification(
                _learningParams,
                _inputVectorSize, 
                builtLayers.Concat(new[] { _output }).ToArray());
            
            int id = 1;

            return new MultilayerNetworkTrainingContext<NetworkSpecification>(() => Interlocked.Increment(ref id), new MultilayerNetwork(spec), spec);
        }
    }
}