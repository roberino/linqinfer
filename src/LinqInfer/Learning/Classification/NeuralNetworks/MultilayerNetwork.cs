using LinqInfer.Data;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class MultilayerNetwork :
        ICloneableObject<MultilayerNetwork>,
        IHasNetworkTopology,
        IVectorClassifier,
        ISerialisableDataTransformation
    {
        INetworkSignalFilter _rootLayer;
        bool _initd;

        public MultilayerNetwork(NetworkSpecification specification, IDictionary<string, string> properties = null)
        {
            specification.Validate();

            Specification = specification;

            Properties = properties ?? new Dictionary<string, string>();

            _initd = false;
        }

        public IDictionary<string, string> Properties { get; }

        public NetworkSpecification Specification { get; }

        public IEnumerable<Matrix> ExportRawData()
        {
            return ForEachLayer(l => l.ExportData(), false);
        }

        public IEnumerable<T> ForEachLayer<T>(Func<ILayer, T> func, bool reverse = true)
        {
            return (reverse ? Layers.Reverse() : Layers).ForEach(func);
        }

        /// <summary>
        /// Applys the network over an input vector
        /// </summary>
        public IVector Evaluate(IVector input)
        {
            if (!_initd) InitialiseLayers();

            return _rootLayer.Process(input);
        }

        /// <summary>
        /// Transforms the vector (same as evaluate)
        /// </summary>
        public IVector Apply(IVector vector)
        {
            return Evaluate(vector);
        }

        public IEnumerable<ILayer> Layers
        {
            get
            {
                if (!_initd) InitialiseLayers();

                var next = _rootLayer as ILayer;

                while (next != null)
                {
                    yield return next;

                    next = next.Successor as ILayer;
                }
            }
        }

        public int InputSize => Specification.InputVectorSize;

        public int OutputSize => Specification.OutputVectorSize;

        /// <summary>
        /// Exports the raw data
        /// </summary>
        public PortableDataDocument ExportData()
        {
            return new MultilayerNetworkExporter().Export(this);
        }

        public void ImportData(PortableDataDocument doc)
        {
            var nn = CreateFromData(doc);

            _rootLayer = nn._rootLayer;
        }

        public static MultilayerNetwork CreateFromData(PortableDataDocument doc)
        {
            return new MultilayerNetworkExporter().Import(doc);
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var layer in Layers)
            {
                s += "[Layer " + layer.Size + "]";
            }

            return $"Network({Specification.InputVectorSize}):{s}";
        }

        public MultilayerNetwork Clone(bool deep)
        {
            var data = ExportData();
            var newNet = new MultilayerNetwork(Specification);

            newNet.ImportData(data);

            return newNet;
        }

        public object Clone()
        {
            return Clone(true);
        }

        public Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            return new NetworkTopologyBuilder(this).BuildAsync(visualSettings, store);
        }

        void InitialiseLayers()
        {
            _rootLayer = new NetworkConfigurationBuilder(Specification)
                .CreateConfiguration();

            _initd = true;
        }
    }
}