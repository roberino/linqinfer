using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetwork : 
        ICloneableObject<MultilayerNetwork>, 
        IBinaryPersistable, 
        IExportableAsVectorDocument, 
        IImportableAsVectorDocument, 
        IHasNetworkTopology,
        IVectorClassifier
    {
        private readonly Func<int, Range, INeuron> _neuronFactory;

        private IDictionary<string, string> _properties;
        private INetworkSignalFilter _rootLayer;
        private NetworkParameters _parameters;
        private NetworkSpecification _specification;
        private bool _initd;

        public MultilayerNetwork(Stream input)
        {
            var n = LoadData(input);
            _neuronFactory = n._neuronFactory;
            _rootLayer = n._rootLayer;
            _parameters = n._parameters;
            _properties = n._properties;
            _initd = true;
        }

        public MultilayerNetwork(NetworkParameters parameters, IDictionary<string, string> properties = null)
        {
            parameters.Validate();

            _parameters = parameters;
            _specification = parameters.ToSpecification();
            _properties = properties ?? new Dictionary<string, string>();

            _initd = false;
        }

        public MultilayerNetwork(NetworkSpecification specification, IDictionary<string, string> properties = null)
        {
            specification.Validate();

            _parameters = specification.ToParameters();
            _specification = specification;
            _properties = properties ?? new Dictionary<string, string>();

            _initd = false;
        }

        private MultilayerNetwork(NetworkParameters parameters, INetworkSignalFilter rootLayer)
        {
            _parameters = parameters;
            _rootLayer = rootLayer;
            _specification = parameters.ToSpecification();
            _initd = true;
        }

        public IDictionary<string, string> Properties => _properties;

        public NetworkSpecification Specification => _specification;

        public NetworkParameters Parameters => _parameters;

        public IEnumerable<Matrix> ExportData()
        {
            return ForEachLayer(l => l.ExportData(), false);
        }

        public async Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            List<WeightedGraphNode<string, double>> previousVertexes = null;
            List<WeightedGraphNode<string, double>> nextVertexes = new List<WeightedGraphNode<string, double>>();
            List<INeuron> currentNeurons = new List<INeuron>();

            var vs = visualSettings ?? new VisualSettings();

            int l = 0;

            var width = vs.Bounds.X;
            var height = vs.Bounds.Y;
            var numLayers = Layers.Count();
            var mSize = Layers.Max(x => x.Size);
            var unitW = width / numLayers;
            var unitH = height / mSize;
            var maxWeight = AllNeurons().Max(n => n.Export().Sum);

            foreach (var layer in Layers.Reverse())
            {
                int i = 0;

                var colour = vs.Palette.GetColourByIndex(l);

                currentNeurons.Clear();

                layer.ForEachNeuron(n =>
                {
                    currentNeurons.Add(n);
                    return 1;
                })
                .ToList();

                var offsetY = (layer.Size - mSize) / 2d * unitH;

                foreach (var n in currentNeurons)
                {
                    i++;

                    var name = l == 0 ? "Output " + i : l == numLayers - 1 ? "Input " + i : "N " + l + "." + i;
                    var node = await graph.FindOrCreateVertexAsync(name);
                    var attribs = await node.GetAttributesAsync();
                    
                    var weights = n.Export();
                    var wsum = Math.Abs(weights.Sum);

                    attribs["weights"] = weights.ToJson();

                    var colourFactor = (float)(wsum / maxWeight);

                    await node.SetPositionAndSizeAsync(vs.Origin.X + width - unitW * l, vs.Origin.Y + unitH * i - offsetY, 0, Math.Min(unitH, unitW) / 2);
                    await node.SetColourAsync(colour);

                    if (previousVertexes != null)
                    {
                        foreach (var vertex in previousVertexes)
                        {
                            await node.ConnectToAsync(vertex, wsum);
                        }
                    }

                    nextVertexes.Add(node);
                }

                previousVertexes = nextVertexes;

                nextVertexes = new List<WeightedGraphNode<string, double>>();

                l++;
            }

            await graph.SaveAsync();

            return graph;
        }

        private void InitialiseLayers()
        {
            NetworkLayer next = null;
            NetworkLayer lastLayer = null;

            for (int i = 0; i < _specification.Layers.Count; i++)
            {
                var layer = _specification.Layers[i];

                if (i == 0)
                {
                    next = new NetworkLayer(_specification.InputVectorSize, layer);
                    _rootLayer = next;
                }
                else
                {
                    next = new NetworkLayer(lastLayer.Size, layer);
                    lastLayer.Successor = next;
                }

                lastLayer = next;
            }

            _initd = true;
        }

        public ILayer LastLayer { get { return Layers.Reverse().First(); } }

        public IEnumerable<T> ForEachLayer<T>(Func<ILayer, T> func, bool reverse = true)
        {
            if (!_initd) InitialiseLayers();

            return (reverse ? Layers.Reverse() : Layers).ForEach(func);
        }

        /// <summary>
        /// Applys the network over an input vector
        /// </summary>
        public IVector Evaluate(IVector input)
        {
            if (!_initd) InitialiseLayers();

            var res = _rootLayer.Process(input);

            if (_parameters.OutputTransformation == null) return res;

            return _parameters.OutputTransformation.Apply(res.ToColumnVector()).ToColumnVector();
        }

        /// <summary>
        /// Reduces the networks input parameters and associated weights to improve it's efficiency.
        /// </summary>
        /// <param name="inputIndexes">One or more input indexes (base zero)</param>
        public void PruneInputs(params int[] inputIndexes)
        {
            if (inputIndexes == null || inputIndexes.Length == 0) throw new ArgumentException("No inputs recieved");

            var newSize = Enumerable.Range(0, Specification.InputVectorSize).Except(inputIndexes).Count();

            ForEachLayer(l =>
            {
                l.ForEachNeuron(n =>
                {
                    n.PruneWeights(inputIndexes);
                    return 1;
                }).ToList();

                return 1;
            }, false).ToList();

            Parameters.InputVectorSize = newSize;
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

        public void Save(Stream output)
        {
            ToVectorDocument().Save(output);

            output.Flush();
        }

        /// <summary>
        /// Exports the raw data
        /// </summary>
        public BinaryVectorDocument ToVectorDocument()
        {
            return new MultilayerNetworkExporter().Export(this);
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            var nn = CreateFromVectorDocument(doc);

            _parameters = nn._parameters;
            _rootLayer = nn._rootLayer;
        }

        public static MultilayerNetwork CreateFromVectorDocument(BinaryVectorDocument doc)
        {
            return new MultilayerNetworkExporter().Import(doc);
        }

        public void Load(Stream input)
        {
            var nn = LoadData(input);

            _parameters = nn._parameters;
            _rootLayer = nn._rootLayer;
        }

        public static MultilayerNetwork LoadData(Stream input)
        {
            var doc = new BinaryVectorDocument();

            doc.Load(input);

            return CreateFromVectorDocument(doc);
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (var layer in Layers)
            {
                s += "[Layer " + layer.Size + "]";
            }

            return string.Format("Network({0}):{1}", Parameters.InputVectorSize, s);
        }

        public MultilayerNetwork Clone(bool deep)
        {
            return new MultilayerNetwork(_parameters.Clone(deep), _rootLayer.Clone(deep));
        }

        public object Clone()
        {
            return Clone(true);
        }

        private IEnumerable<INeuron> AllNeurons()
        {
            foreach(var layer in Layers)
            {
                var neurons = new List<INeuron>();

                layer.ForEachNeuron(n =>
                {
                    neurons.Add(n);
                    return 1;
                }).ToList();

                foreach (var n in neurons) yield return n;
            }
        }
    }
}