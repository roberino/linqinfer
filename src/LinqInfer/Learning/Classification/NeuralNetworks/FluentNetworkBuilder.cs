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
                .AddHiddenLinearLayer(hiddenLayerSize, p => SimpleWeightUpdateRule.Create(p.LearningRate))
                .AddSoftmaxOutput();
        }

        public static IFluentNetworkBuilder AddHiddenSigmoidLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize)
        {
            if (layerSize == 0) return specificationBuilder;

            return specificationBuilder.
                AddHiddenLayer(new LayerSpecification(layerSize, Activators.Sigmoid(1), LossFunctions.Square));

        }

        public static IFluentNetworkBuilder AddHiddenLinearLayer(this IFluentNetworkBuilder specificationBuilder, int layerSize, Func<LearningParameters, IWeightUpdateRule> updateRule = null)
        {
            if (layerSize == 0) return specificationBuilder;

            updateRule = updateRule ?? (p => DefaultWeightUpdateRule.Create(p.LearningRate, p.Momentum));

            return specificationBuilder.
                AddHiddenLayer(p => 
                    new LayerSpecification(
                        layerSize, 
                        Activators.None(), 
                        LossFunctions.Square,
                        updateRule(p),
                        LayerSpecification.DefaultInitialWeightRange));
        }

        public static IFluentNetworkBuilder AddSoftmaxOutput(this IFluentNetworkBuilder specificationBuilder)
        {
            return specificationBuilder
                .ConfigureOutputLayer(Activators.None(), LossFunctions.CrossEntropy, null, p => SimpleWeightUpdateRule.Create(p.LearningRate))
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
        private readonly IList<Func<LearningParameters, LayerSpecification>> _layers;
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
            _layers = new List<Func<LearningParameters, LayerSpecification>>();
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
            _layers.Add(_ => layer);
            return this;
        }

        public IFluentNetworkBuilder AddHiddenLayer(Func<LearningParameters, LayerSpecification> layerFactory)
        {
            _layers.Add(layerFactory);
            return this;
        }

        public IFluentNetworkBuilder ConfigureOutputLayer(IActivatorFunction activator, ILossFunction lossFunction, Range? initialWeightRange = null, Func<LearningParameters, IWeightUpdateRule> updateRule = null)
        {
            var tx = _output.OutputTransformation;
            
            updateRule = updateRule ?? (p => DefaultWeightUpdateRule.Create(p.LearningRate, p.Momentum));

            _output = new LayerSpecification(
                _output.LayerSize, 
                activator, 
                lossFunction, 
                updateRule(_learningParams), 
                initialWeightRange.GetValueOrDefault(_output.InitialWeightRange))
            {
                OutputTransformation = tx
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(ISerialisableDataTransformation transformation)
        {
            _output = new LayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
            {
                OutputTransformation = transformation
            };

            return this;
        }

        public IFluentNetworkBuilder TransformOutput(Func<int, ISerialisableDataTransformation> transformationFactory)
        {
            _output = new LayerSpecification(_output.LayerSize, _output.Activator, _output.LossFunction, _output.WeightUpdateRule, _output.InitialWeightRange)
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