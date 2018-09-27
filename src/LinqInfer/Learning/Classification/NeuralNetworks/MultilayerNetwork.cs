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

        void InitialiseLayers()
        {
            Specification.Initialise();

            NetworkLayer next = null;
            NetworkLayer lastLayer = null;

            for (int i = 0; i < Specification.Layers.Count; i++)
            {
                var layer = Specification.Layers[i];

                if (i == 0)
                {
                    next = new NetworkLayer(Specification.InputVectorSize, layer);
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

        public ILayer LastLayer => Layers.Reverse().First();

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

        IEnumerable<INeuron> AllNeurons()
        {
            foreach (var layer in Layers)
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