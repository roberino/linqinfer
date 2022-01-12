using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsDataDocument, IEquatable<NetworkSpecification>
    {
        internal NetworkSpecification(TrainingParameters trainingParameters, int inputVectorSize, ILossFunction lossFunction, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(trainingParameters, nameof(trainingParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            TrainingParameters = trainingParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkLayers.Cast<NetworkModuleSpecification>().ToList();
            Output = new NetworkOutputSpecification(networkLayers.Last(), lossFunction);
        }

        internal NetworkSpecification(TrainingParameters trainingParameters, int inputVectorSize, NetworkOutputSpecification output, params NetworkModuleSpecification[] networkModules)
        {
            ArgAssert.AssertNonNull(trainingParameters, nameof(trainingParameters));
            ArgAssert.AssertGreaterThanZero(networkModules.Length, nameof(networkModules.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            TrainingParameters = trainingParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkModules.ToList();
            Output = output;
        }

        internal NetworkSpecification(int inputVectorSize, params NetworkLayerSpecification[] networkLayers) : this(new TrainingParameters(), inputVectorSize, LossFunctions.Square, networkLayers)
        {
        }

        public TrainingParameters TrainingParameters { get; }

        public int InputVectorSize { get; }

        public NetworkModuleSpecification Root => Modules.FirstOrDefault();

        public NetworkOutputSpecification Output { get; }

        public IReadOnlyCollection<NetworkModuleSpecification> Modules { get; }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType<NetworkSpecification>();
            doc.SetPropertyFromExpression(() => TrainingParameters.LearningRate);
            doc.SetPropertyFromExpression(() => TrainingParameters.MinimumError);
            doc.SetPropertyFromExpression(() => InputVectorSize);
            doc.Properties[nameof(Root)] = Root.Id.ToString();

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
            var learningRate = doc.PropertyOrDefault(() => networkSpecification.TrainingParameters.LearningRate, 0.01);
            var minimumError = doc.PropertyOrDefault(() => networkSpecification.TrainingParameters.MinimumError, 0.01);
            var inputVectorSize = doc.PropertyOrDefault(() => networkSpecification.InputVectorSize, 0);

            var layers = doc.FindChildrenByName<NetworkLayerSpecification>()
                .Select(c => NetworkLayerSpecification.FromVectorDocument(c, ctx));

            var modules = doc.FindChildrenByName<NetworkModuleSpecification>()
                .Select(c => NetworkModuleSpecification.FromVectorDocument(c, ctx));

            var output = doc.FindChildrenByName<NetworkOutputSpecification>()
                .Select(c => NetworkOutputSpecification.FromVectorDocument(c, ctx))
                .SingleOrDefault();

            var rootId = doc.PropertyOrDefault(nameof(Root), -1);

            var all = layers
                .Concat(modules)
                .OrderBy(m => m.Id == rootId ? -1 : 0)
                .ThenBy(m => m.Id)
                .ToArray();

            var learningParams = new TrainingParameters()
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
                layer.WeightUpdateRule.Initialise(TrainingParameters);
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
            TrainingParameters.Validate();

            var dups = Modules.GroupBy(m => m.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

            if (dups.Any())
            {
                throw new ArgumentException($"Duplicate modules: {string.Join(",", dups)}");
            }
        }
    }
}