using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class LayerSpecification : IExportableAsDataDocument
    {
        public static readonly Range DefaultInitialWeightRange = new Range(0.00000001, -0.00000001);

        private ISerialisableDataTransformation _outputTransformation;

        public LayerSpecification(
            int layerSize, 
            ActivatorExpression activator, 
            ILossFunction lossFunction,
            WeightUpdateRule weightUpdateRule,
            Range initialWeightRange,
            bool parallelProcess = false,
            Func<int, INeuron> neuronFactory = null)
        {
            ArgAssert.AssertGreaterThanZero(layerSize, nameof(layerSize));
            ArgAssert.AssertNonNull(activator, nameof(activator));
            ArgAssert.AssertNonNull(lossFunction, nameof(lossFunction));

            LayerSize = layerSize;
            Activator = activator;
            LossFunction = lossFunction;
            WeightUpdateRule = weightUpdateRule;
            InitialWeightRange = initialWeightRange;
            ParallelProcess = parallelProcess;
            NeuronFactory = neuronFactory ?? (x => new NeuronBase(x, InitialWeightRange));
        }

        public LayerSpecification(
            int layerSize,
            ActivatorExpression activator = null,
            ILossFunction lossFunction = null) : this(
                layerSize, activator ?? Activators.Sigmoid(1), 
                lossFunction ?? LossFunctions.Square, 
                WeightUpdateRules.Default(), 
                DefaultInitialWeightRange)
        {
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
        /// Transforms the output
        /// </summary>
        public ISerialisableDataTransformation OutputTransformation
        {
            get => _outputTransformation;
            set
            {
                if (value != null && value.InputSize != LayerSize)
                {
                    throw new ArgumentException(nameof(value.InputSize));
                }
                _outputTransformation = value;
            }
        }

        /// <summary>
        /// Gets or sets the initial weight range used to initialise neurons
        /// </summary>
        public Range InitialWeightRange { get; }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetPropertyFromExpression(() => LayerSize);
            doc.SetPropertyFromExpression(() => Activator, Activator.Export());
            doc.SetPropertyFromExpression(() => LossFunction, LossFunction.GetType().Name);
            doc.SetPropertyFromExpression(() => WeightUpdateRule, WeightUpdateRule.Export());

            doc.Properties["InitialWeightRangeMin"] = InitialWeightRange.Min.ToString();
            doc.Properties["InitialWeightRangeMax"] = InitialWeightRange.Max.ToString();

            if (OutputTransformation != null)
            {
                doc.WriteChildObject(OutputTransformation, new
                {
                    Property = nameof(OutputTransformation)
                });
            }

            return doc;
        }

        internal static LayerSpecification FromVectorDocument(PortableDataDocument doc, NetworkBuilderContext context)
        {
            LayerSpecification layer = null;

            var layerSize = doc.PropertyOrDefault(() => layer.LayerSize, 0);
            var activatorStr = doc.PropertyOrDefault(() => layer.Activator, string.Empty);
            var lossFuncStr = doc.PropertyOrDefault(() => layer.LossFunction, string.Empty);
            var weightUpdateRuleStr = doc.PropertyOrDefault(() => layer.WeightUpdateRule, string.Empty);

            var initRangeMin = doc.PropertyOrDefault("InitialWeightRangeMin", 0.0d);
            var initRangeMax = doc.PropertyOrDefault("InitialWeightRangeMax", 0.0d);

            var activator = context.ActivatorFactory.Create(activatorStr);
            var lossFunc = context.LossFunctionFactory.Create(lossFuncStr);
            var wuRule = context.WeightUpdateRuleFactory.Create(weightUpdateRuleStr);

            ISerialisableDataTransformation outputTransform = null;

            if (doc.Children.Count > 0)
            {
                var query = doc.QueryChildren(new { Property = nameof(OutputTransformation) }).SingleOrDefault();

                if (query != null)
                {
                    outputTransform = doc.ReadChildObject(context.TransformationFactory.Create(query.TypeName));
                }
            }

            return new LayerSpecification(layerSize, activator, lossFunc, wuRule, new Range(initRangeMax, initRangeMin))
            {
                OutputTransformation = outputTransform
            };
        }
    }
}