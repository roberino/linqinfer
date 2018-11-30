using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsDataDocument, IEquatable<NetworkSpecification>
    {
        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, ILossFunction lossFunction, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkLayers.Cast<NetworkModuleSpecification>().ToList();
            Output = new NetworkOutputSpecification(networkLayers.Last(), lossFunction);
        }

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, NetworkOutputSpecification output, params NetworkModuleSpecification[] networkModules)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkModules.Length, nameof(networkModules.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkModules.ToList();
            Output = output;
        }

        public NetworkSpecification(int inputVectorSize, params NetworkLayerSpecification[] networkLayers) : this(new LearningParameters(), inputVectorSize, LossFunctions.Square, networkLayers)
        {
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }

        public NetworkFlowModel NetworkFlowModel { get; set; } = NetworkFlowModel.Convolutional;

        public NetworkModuleSpecification Root => Modules.FirstOrDefault();

        public NetworkOutputSpecification Output { get; }

        public IReadOnlyCollection<NetworkModuleSpecification> Modules { get; }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType<NetworkSpecification>();
            doc.SetPropertyFromExpression(() => LearningParameters.LearningRate);
            doc.SetPropertyFromExpression(() => LearningParameters.MinimumError);
            doc.SetPropertyFromExpression(() => InputVectorSize);

            foreach (var child in Modules)
            {
                doc.Children.Add(child.ExportData());
            }

            doc.Children.Add(Output.ExportData());

            return doc;
        }

        internal static NetworkSpecification FromDataDocument(
            PortableDataDocument doc,
            NetworkBuilderContext context = null)
        {
            NetworkSpecification networkSpecification = null;

            var ctx = context ?? new NetworkBuilderContext();
            var learningRate = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.LearningRate, 0.01);
            var minimumError = doc.PropertyOrDefault(() => networkSpecification.LearningParameters.MinimumError, 0.01);
            var inputVectorSize = doc.PropertyOrDefault(() => networkSpecification.InputVectorSize, 0);

            var layers = doc.FindChildrenByName<NetworkLayerSpecification>()
                .Select(c => NetworkLayerSpecification.FromVectorDocument(c, ctx));

            var modules = doc.FindChildrenByName<NetworkModuleSpecification>()
                .Select(c => NetworkModuleSpecification.FromVectorDocument(c, ctx));

            var output = doc.FindChildrenByName<NetworkOutputSpecification>()
                .Select(c => NetworkOutputSpecification.FromVectorDocument(c, ctx))
                .SingleOrDefault();

            var all = layers.Concat(modules).ToArray();

            var learningParams = new LearningParameters()
            {
                LearningRate = learningRate,
                MinimumError = minimumError
            };

            return new NetworkSpecification(learningParams, inputVectorSize, output, all)
                .Initialise();
        }

        public NetworkSpecification Initialise()
        {
            Validate();

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

        internal IEnumerable<NetworkLayerSpecification> Layers =>
            Modules.Where(m => m is NetworkLayerSpecification)
                .Cast<NetworkLayerSpecification>();

        internal void Validate()
        {
            LearningParameters.Validate();

            var dups = Modules.GroupBy(m => m.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (dups.Any())
            {
                throw new ArgumentException($"Duplicate modules: {string.Join(",", dups)}");
            }
        }
    }
}