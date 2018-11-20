using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkLayerSpecification : NetworkModuleSpecification, IExportableAsDataDocument
    {
        public static readonly Range DefaultInitialWeightRange = new Range(0.0001, -0.0001);

        internal NetworkLayerSpecification(
            int id,
            int layerSize,
            ActivatorExpression activator = null,
            ILossFunction lossFunction = null,
            WeightUpdateRule weightUpdateRule = null,
            Range? initialWeightRange = null,
            bool parallelProcess = false,
            Func<int, INeuron> neuronFactory = null) : base(id)
        {
            ArgAssert.AssertGreaterThanZero(layerSize, nameof(layerSize));

            LayerSize = layerSize;
            Activator = activator ?? Activators.Sigmoid(1);
            LossFunction = lossFunction ?? LossFunctions.Square;
            WeightUpdateRule = weightUpdateRule ?? WeightUpdateRules.Default();
            InitialWeightRange = initialWeightRange.GetValueOrDefault(DefaultInitialWeightRange);
            ParallelProcess = parallelProcess;
            NeuronFactory = neuronFactory ?? (x => new NeuronBase(x, InitialWeightRange));
        }

        public override ISerialisableDataTransformation OutputTransformation
        {
            get => base.OutputTransformation;
            set
            {
                if (value != null && value.InputSize != LayerSize)
                {
                    throw new ArgumentException(nameof(value.InputSize));
                }
                base.OutputTransformation = value;
            }
        }

        /// <summary>
        /// The number of neurons in each layer
        /// </summary>
        public int LayerSize { get; }

        /// <summary>
        /// When true, the layer should use a parallel processing model
        /// </summary>
        public bool ParallelProcess { get; internal set; }

        /// <summary>
        /// Returns a factory function for creating new neurons
        /// </summary>
        public Func<int, INeuron> NeuronFactory { get; }

        /// <summary>
        /// Returns a function for calculating errors
        /// </summary>
        public ILossFunction LossFunction { get; }

        /// <summary>
        /// Gets the activator function
        /// </summary>
        public ActivatorExpression Activator { get; }

        /// <summary>
        /// Gets a function for updating weights
        /// </summary>
        public WeightUpdateRule WeightUpdateRule { get; }

        /// <summary>
        /// Gets or sets the initial weight range used to initialise neurons
        /// </summary>
        public Range InitialWeightRange { get; }

        public override PortableDataDocument ExportData()
        {
            var doc = base.ExportData();

            doc.SetName(nameof(NetworkLayerSpecification));
            doc.SetPropertyFromExpression(() => LayerSize);
            doc.SetPropertyFromExpression(() => Activator, Activator.Export());
            doc.SetPropertyFromExpression(() => LossFunction, LossFunction.GetType().Name);
            doc.SetPropertyFromExpression(() => WeightUpdateRule, WeightUpdateRule.Export());

            doc.Properties["InitialWeightRangeMin"] = InitialWeightRange.Min.ToString();
            doc.Properties["InitialWeightRangeMax"] = InitialWeightRange.Max.ToString();

            return doc;
        }

        internal new static NetworkLayerSpecification FromVectorDocument(PortableDataDocument doc, NetworkBuilderContext context)
        {
            var moduleSpec = NetworkModuleSpecification.FromVectorDocument(doc, context);
            NetworkLayerSpecification networkLayer = null;

            var layerSize = doc.PropertyOrDefault(() => networkLayer.LayerSize, 0);
            var activatorStr = doc.PropertyOrDefault(() => networkLayer.Activator, string.Empty);
            var lossFuncStr = doc.PropertyOrDefault(() => networkLayer.LossFunction, string.Empty);
            var weightUpdateRuleStr = doc.PropertyOrDefault(() => networkLayer.WeightUpdateRule, string.Empty);

            var initRangeMin = doc.PropertyOrDefault("InitialWeightRangeMin", 0.0d);
            var initRangeMax = doc.PropertyOrDefault("InitialWeightRangeMax", 0.0d);

            var activator = context.ActivatorFactory.Create(activatorStr);
            var lossFunc = context.LossFunctionFactory.Create(lossFuncStr);
            var wuRule = context.WeightUpdateRuleFactory.Create(weightUpdateRuleStr);

            var layerSpec = new NetworkLayerSpecification(moduleSpec.Id, layerSize, activator, lossFunc, wuRule, new Range(initRangeMax, initRangeMin))
            {
                OutputTransformation = moduleSpec.OutputTransformation,
                InputOperator = moduleSpec.InputOperator
            };

            layerSpec.Connections.Inputs.Add(moduleSpec.Connections.Inputs);
            layerSpec.Connections.Outputs.Add(moduleSpec.Connections.Outputs);

            return layerSpec;
        }
    }
}