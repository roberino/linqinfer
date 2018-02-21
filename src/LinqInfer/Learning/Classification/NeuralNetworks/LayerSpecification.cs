using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class LayerSpecification : IExportableAsVectorDocument
    {
        public LayerSpecification(
            int layerSize, 
            ActivatorFunc activator, 
            ILossFunction lossFunction,
            Range initialWeightRange,
            Func<int, INeuron> neuronFactory = null)
        {
            ArgAssert.AssertGreaterThanZero(layerSize, nameof(layerSize));
            ArgAssert.AssertNonNull(activator, nameof(activator));
            ArgAssert.AssertNonNull(lossFunction, nameof(lossFunction));

            LayerSize = layerSize;
            Activator = activator;
            LossFunction = lossFunction;
            InitialWeightRange = initialWeightRange;
            NeuronFactory = neuronFactory ?? (x => new NeuronBase(x, InitialWeightRange));
        }

        /// <summary>
        /// The number of neurons in each layer
        /// </summary>
        public int LayerSize { get; }

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
        public ActivatorFunc Activator { get; }

        /// <summary>
        /// Transforms the output
        /// </summary>
        public SerialisableVectorTransformation OutputTransformation { get; set; }

        /// <summary>
        /// Gets or sets the initial weight range used to initialise neurons
        /// </summary>
        public Range InitialWeightRange { get; }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.SetPropertyFromExpression(() => LayerSize);
            doc.SetPropertyFromExpression(() => Activator);
            doc.SetPropertyFromExpression(() => LossFunction, LossFunction.GetType().Name);

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

        internal static LayerSpecification FromVectorDocument(BinaryVectorDocument doc, NetworkBuilderContext context)
        {
            LayerSpecification layer = null;

            var layerSize = doc.PropertyOrDefault(() => layer.LayerSize, 0);
            var activatorStr = doc.PropertyOrDefault(() => layer.Activator, string.Empty);
            var lossFuncStr = doc.PropertyOrDefault(() => layer.LossFunction, string.Empty);

            var initRangeMin = doc.PropertyOrDefault("InitialWeightRangeMin", 0.0d);
            var initRangeMax = doc.PropertyOrDefault("InitialWeightRangeMax", 0.0d);

            var activator = context.ActivatorFactory.Create(activatorStr);
            var lossFunc = context.LossFunctionFactory.Create(lossFuncStr);

            SerialisableVectorTransformation outputTransform = null;

            if (doc.Children.Count > 0 && doc.QueryChildren(new { Property = nameof(OutputTransformation) }).Any())
            {
                outputTransform = doc.ReadChildObject(new SerialisableVectorTransformation());
            }

            return new LayerSpecification(layerSize, activator, lossFunc, new Range(initRangeMax, initRangeMin))
            {
                OutputTransformation = outputTransform
            };
        }
    }
}