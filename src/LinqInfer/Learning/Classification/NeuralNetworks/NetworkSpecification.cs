using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsDataDocument, IEquatable<NetworkSpecification>
    {
        public NetworkSpecification(LearningParameters learningParameters, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));

            LearningParameters = learningParameters;
            InputVectorSize = networkLayers.First().LayerSize;
            OutputVectorSize = networkLayers.Last().LayerSize;
            Layers = networkLayers.ToList();
        }

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            OutputVectorSize = networkLayers.Last().LayerSize;
            Layers = networkLayers.ToList();
        }

        public NetworkSpecification(int inputVectorSize, params NetworkLayerSpecification[] networkLayers) : this(new LearningParameters(), inputVectorSize, networkLayers)
        {
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }
        public int OutputVectorSize { get; }

        public IList<NetworkLayerSpecification> Layers { get; }

        public NetworkLayerSpecification OutputLayer => Layers.Last();

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType<NetworkSpecification>();
            doc.SetPropertyFromExpression(() => LearningParameters.LearningRate);
            doc.SetPropertyFromExpression(() => LearningParameters.MinimumError);
            doc.SetPropertyFromExpression(() => InputVectorSize);

            foreach (var child in Layers)
            {
                doc.Children.Add(child.ExportData());
            }

            return doc;
        }

        internal static NetworkSpecification FromVectorDocument(PortableDataDocument doc,
            NetworkBuilderContext context = null)
        {
            NetworkSpecification networkSpecification = null;

            var ctx = context ?? new NetworkBuilderContext();
            var learningRate = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.LearningRate, 0.01);
            var minimumError = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.MinimumError, 0.01);
            var inputVectorSize = doc.PropertyOrDefault(() => networkSpecification.InputVectorSize, 0);

            var layers = doc.Children.Select(c => NetworkLayerSpecification.FromVectorDocument(c, ctx)).ToArray();

            var learningParams = new LearningParameters()
            {
                LearningRate = learningRate,
                MinimumError = minimumError
            };

            return (inputVectorSize > 0
                ? new NetworkSpecification(learningParams, inputVectorSize, layers)
                : new NetworkSpecification(learningParams, layers)).Initialise();
        }

        public NetworkSpecification Initialise()
        {
            foreach (var layer in Layers)
            {
                layer.WeightUpdateRule.Initialise(LearningParameters);
            }

            return this;
        }

        public bool Equals(NetworkSpecification other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            var docA = ExportData();
            var docB = other.ExportData();

            return docA.Equals(docB);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as NetworkSpecification);
        }

        public override int GetHashCode()
        {
            return (int)ExportData().Checksum;
        }

        internal void Validate()
        {
            LearningParameters.Validate();
        }
    }
}