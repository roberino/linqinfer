using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetwork : 
        ICloneableObject<MultilayerNetwork>, 
        IBinaryPersistable, 
        IExportableAsVectorDocument, 
        IImportableAsVectorDocument, 
        IHasNetworkTopology
    {
        private readonly Func<int, Range, INeuron> _neuronFactory;

        private IDictionary<string, string> _properties;
        private INetworkSignalFilter _rootLayer;
        private NetworkParameters _parameters;
        private bool initd;

        public MultilayerNetwork(Stream input)
        {
            var n = LoadData(input);
            _neuronFactory = n._neuronFactory;
            _rootLayer = n._rootLayer;
            _parameters = n._parameters;
            _properties = n._properties;
            initd = true;
        }

        public MultilayerNetwork(NetworkParameters parameters, IDictionary<string, string> properties = null)
        {
            parameters.Validate();

            _parameters = parameters;
            _properties = properties ?? new Dictionary<string, string>();

            initd = false;
        }

        internal MultilayerNetwork(int inputVectorSize, int[] neuronSizes, ActivatorFunc activator = null, Func<int, Range, INeuron> neuronFactory = null)
        {
            _neuronFactory = neuronFactory;

            _parameters = new NetworkParameters(new int[] { inputVectorSize }.Concat(neuronSizes).ToArray(), activator);
            _properties = new Dictionary<string, string>();

            initd = false;
        }

        private MultilayerNetwork(NetworkParameters parameters, Func<int, Range, INeuron> neuronFactory, INetworkSignalFilter rootLayer, int inputVectorSize)
        {
            _parameters = parameters;
            _neuronFactory = neuronFactory;
            _rootLayer = rootLayer;
            initd = true;
        }

        public IDictionary<string, string> Properties
        {
            get { return _properties; }
        }

        public NetworkParameters Parameters
        {
            get
            {
                return _parameters;
            }
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
            var maxWeight = AllNeurons().Max(n => n.Export().Sum());

            foreach (var layer in Layers.Reverse())
            {
                int i = 0;

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

                    var name = l == 0 ? "Output " + i : l == numLayers - 1 ? "Input " + i : "N " + i;
                    var node = await graph.FindOrCreateVertexAsync(name);
                    var attribs = await node.GetAttributesAsync();
                    
                    var weights = n.Export();

                    attribs["weights"] = weights.ToJson();

                    var colour = (byte)(weights.Sum() / maxWeight * 255);

                    await node.SetPositionAndSizeAsync(vs.Origin.X + width - unitW * l, vs.Origin.Y + unitH * i - offsetY, 0, Math.Min(unitH, unitW) / 2);
                    await node.SetColourAsync(colour, 0, 0);

                    if (previousVertexes != null)
                    {
                        foreach (var vertex in previousVertexes)
                        {
                            await node.ConnectToAsync(vertex, weights.Sum());
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
            INetworkSignalFilter next = null;

            int lastN = 0;

            Func<int, INeuron> neuronFactory;

            if (_neuronFactory == null)
                neuronFactory = (x => new NeuronBase(x, _parameters.InitialWeightRange));
            else
                neuronFactory = (x => _neuronFactory(x, _parameters.InitialWeightRange));

            foreach (var n in _parameters.LayerSizes.Where(x => x > 0)) // Don't create empty layers
            {
                var prev = next;

                if (prev == null)
                {
                    next = new NetworkLayer(Parameters.InputVectorSize, Parameters.InputVectorSize, _parameters.Activator, neuronFactory);
                    _rootLayer = next;
                }
                else
                {
                    next = new NetworkLayer(lastN, n, _parameters.Activator, neuronFactory);
                    prev.Successor = next;
                }

                lastN = n;
            }

            initd = true;
        }

        public ILayer LastLayer { get { return Layers.Reverse().First(); } }

        public IEnumerable<T> ForEachLayer<T>(Func<ILayer, T> func, bool reverse = true)
        {
            if (!initd) InitialiseLayers();

            return (reverse ? Layers.Reverse() : Layers).Select(l => func(l));
        }

        public ColumnVector1D Evaluate(ColumnVector1D input)
        {
            if (!initd) InitialiseLayers();

            var res = _rootLayer.Process(input);

            // Debugger.Log("{0} => {1}", input.ToCsv(2), res.ToCsv(2));

            return res;
        }

        /// <summary>
        /// Reduces the networks input parameters and associated weights to improve it's efficiency.
        /// </summary>
        /// <param name="inputIndexes">One or more input indexes (base zero)</param>
        public void PruneInputs(params int[] inputIndexes)
        {
            if (inputIndexes == null || inputIndexes.Length == 0) throw new ArgumentException("No inputs recieved");

            var newSize = Enumerable.Range(0, Parameters.InputVectorSize).Except(inputIndexes).Count();

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
                if (!initd) InitialiseLayers();

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
            var doc = new BinaryVectorDocument();

            foreach (var prop in _properties)
            {
                doc.Properties["_" + prop.Key] = prop.Value;
            }

            doc.Properties["Activator"] = _parameters.Activator.Name;
            doc.Properties["ActivatorParameter"] = _parameters.Activator.Parameter.ToString();
            doc.Properties["InitialWeightRangeMin"] = _parameters.InitialWeightRange.Min.ToString();
            doc.Properties["InitialWeightRangeMax"] = _parameters.InitialWeightRange.Max.ToString();
            doc.Properties["LearningRate"] = _parameters.LearningRate.ToString();

            doc.Properties["Label"] = "Network";

            int i = 0;

            foreach (var layer in Layers)
            {
                i++;

                var layerDoc = layer.Export();

                layerDoc.Properties["Label"] = "Layer " + i;

                doc.Children.Add(layerDoc);
            }

            return doc;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            var nn = CreateFromVectorDocument(doc);

            _parameters = nn._parameters;
            _rootLayer = nn._rootLayer;
        }

        public static MultilayerNetwork CreateFromVectorDocument(BinaryVectorDocument doc)
        {
            var activator = Activators.Create(doc.Properties["Activator"], double.Parse(doc.Properties["ActivatorParameter"]));
            var layerSizes = doc.Children.Select(c => int.Parse(c.Properties["Size"])).ToArray();

            var properties = doc.Properties.Where(p => p.Key.StartsWith("_")).ToDictionary(p => p.Key.Substring(1), p => p.Value);

            var network = new MultilayerNetwork(new NetworkParameters(layerSizes, activator)
            {
                LearningRate = double.Parse(doc.Properties["LearningRate"]),
                InitialWeightRange = new Range(double.Parse(doc.Properties["InitialWeightRangeMax"]), double.Parse(doc.Properties["InitialWeightRangeMin"]))
            }, properties);

            int i = 0;

            foreach (var layer in network.Layers)
            {
                layer.Import(doc.Children[i++]);
            }

            return network;
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
            return new MultilayerNetwork(_parameters.Clone(deep), _neuronFactory, _rootLayer.Clone(deep), Parameters.InputVectorSize);
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