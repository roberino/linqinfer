using LinqInfer.Data;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsVectorDocument, IEquatable<NetworkSpecification>
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

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, params LayerSpecification[] layers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(layers.Length, nameof(layers.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            OutputVectorSize = layers.Last().LayerSize;
            Layers = layers.ToList();
        }

        public NetworkSpecification(int inputVectorSize, params LayerSpecification[] layers) : this(new LearningParameters(), inputVectorSize, layers)
        {
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }
        public int OutputVectorSize { get; }

        public IList<LayerSpecification> Layers { get; }

        public LayerSpecification OutputLayer => Layers.Last();

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.SetType<NetworkSpecification>();
            doc.SetPropertyFromExpression(() => LearningParameters.LearningRate);
            doc.SetPropertyFromExpression(() => LearningParameters.MinimumError);
            doc.SetPropertyFromExpression(() => InputVectorSize);

            foreach (var child in Layers)
            {
                doc.Children.Add(child.ToVectorDocument());
            }

            return doc;
        }

        internal static NetworkSpecification FromVectorDocument(BinaryVectorDocument doc, NetworkBuilderContext context = null)
        {
            NetworkSpecification networkSpecification = null;

            var ctx = context ?? new NetworkBuilderContext();
            var learningRate = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.LearningRate, 0.01);
            var minimumError = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.MinimumError, 0.01);
            var inputVectorSize = doc.PropertyOrDefault(() => networkSpecification.InputVectorSize, 0);

            var layers = doc.Children.Select(c => LayerSpecification.FromVectorDocument(c, ctx)).ToArray();

            var learningParams = new LearningParameters()
            {
                LearningRate = learningRate,
                MinimumError = minimumError
            };

            if (layers.Length == 1) return new NetworkSpecification(learningParams, inputVectorSize, layers.Single());

            return new NetworkSpecification(learningParams, layers);
        }

        internal void Validate()
        {
            LearningParameters.Validate();
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

        public bool Equals(NetworkSpecification other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            var docA = ToVectorDocument();
            var docB = other.ToVectorDocument();

            return docA.Equals(docB);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NetworkSpecification);
        }

        public override int GetHashCode()
        {
            return (int)ToVectorDocument().Checksum;
        }
    }
}