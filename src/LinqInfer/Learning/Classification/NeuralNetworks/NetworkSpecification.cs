using LinqInfer.Data;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsVectorDocument
    {
        public NetworkSpecification(LearningParameters learningParameters, params LayerSpecification[] layers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(layers.Length, nameof(layers.Length));

            LearningParameters = learningParameters;
            InputVectorSize = layers.First().LayerSize;
            OutputVectorSize = layers.Last().LayerSize;
            Layers = layers.ToList();
        }

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, LayerSpecification outputLayer)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertNonNull(outputLayer, nameof(outputLayer));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            OutputVectorSize = outputLayer.LayerSize;
            Layers = new List<LayerSpecification>() { outputLayer };
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }
        public int OutputVectorSize { get; }

        public IList<LayerSpecification> Layers { get; }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.SetPropertyFromExpression(() => LearningParameters.LearningRate);
            doc.SetPropertyFromExpression(() => LearningParameters.MinimumError);

            foreach (var child in Layers)
            {
                doc.Children.Add(child.ToVectorDocument());
            }

            return doc;
        }

        internal void Validate()
        {
        }

        internal NetworkParameters ToParameters()
        {
            var layerSizes = new List<int>();

            if (Layers.Count == 1)
            {
                layerSizes.Add(InputVectorSize);
            }

            layerSizes.AddRange(Layers.Select(l => l.LayerSize));

            return new NetworkParameters(layerSizes.ToArray(), Layers.Last().Activator)
            {
                InitialWeightRange = Layers.Last().InitialWeightRange,
                LearningRate = LearningParameters.LearningRate,
                MinimumError = LearningParameters.MinimumError
            };
        }
    }
}