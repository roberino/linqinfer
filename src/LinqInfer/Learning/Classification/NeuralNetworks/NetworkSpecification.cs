using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    public sealed class NetworkSpecification : IExportableAsDataDocument, IEquatable<NetworkSpecification>
    {
        int? _fixedOutputSize;
        NetworkModuleSpecification _output;

        public NetworkSpecification(LearningParameters learningParameters, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));

            LearningParameters = learningParameters;
            InputVectorSize = networkLayers.First().LayerSize;
            Modules = networkLayers.Cast<NetworkModuleSpecification>().ToList();

            // OutputVectorSize = networkLayers.Last().LayerSize;
        }

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, params NetworkLayerSpecification[] networkLayers)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkLayers.Length, nameof(networkLayers.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkLayers.Cast<NetworkModuleSpecification>().ToList();
        }

        public NetworkSpecification(LearningParameters learningParameters, int inputVectorSize, params NetworkModuleSpecification[] networkModules)
        {
            ArgAssert.AssertNonNull(learningParameters, nameof(learningParameters));
            ArgAssert.AssertGreaterThanZero(networkModules.Length, nameof(networkModules.Length));
            ArgAssert.AssertGreaterThanZero(inputVectorSize, nameof(inputVectorSize));

            LearningParameters = learningParameters;
            InputVectorSize = inputVectorSize;
            Modules = networkModules.ToList();
        }

        public NetworkSpecification(int inputVectorSize, params NetworkLayerSpecification[] networkLayers) : this(new LearningParameters(), inputVectorSize, networkLayers)
        {
        }

        public LearningParameters LearningParameters { get; }

        public int InputVectorSize { get; }

        public int OutputVectorSize
        {
            get
            {
                if(_fixedOutputSize.HasValue)
                {
                    return
                        _fixedOutputSize.Value;
                }

                if (Output is NetworkLayerSpecification nl)
                {
                    return nl.LayerSize;
                }

                return 0;
            }
        }

        public NetworkFlowModel NetworkFlowModel { get; set; } = NetworkFlowModel.Convolutional;

        public NetworkModuleSpecification Root => Modules.FirstOrDefault();

        public NetworkModuleSpecification Output
        {
            get => _output ?? Modules.LastOrDefault();
        }

        public void SetOutput(NetworkModuleSpecification networkModule, int outputSize)
        {
            if (!Modules.Contains(networkModule))
            {
                throw new ArgumentException();
            }

            _fixedOutputSize = outputSize;
            _output = networkModule;
        }

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

            var layers = doc.Children.Select(c => 
                string.Equals(c.Name, nameof(NetworkModuleSpecification), StringComparison.OrdinalIgnoreCase) ? 
                NetworkModuleSpecification.FromVectorDocument(c, ctx) : 
                NetworkLayerSpecification.FromVectorDocument(c, ctx)).ToArray();

            var learningParams = new LearningParameters()
            {
                LearningRate = learningRate,
                MinimumError = minimumError
            };

            return (inputVectorSize > 0
                ? new NetworkSpecification(learningParams, inputVectorSize, layers)
                : new NetworkSpecification(learningParams, layers.Cast<NetworkLayerSpecification>().ToArray()))
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