using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkTopologyBuilder
    {
        readonly MultilayerNetwork _network;

        public NetworkTopologyBuilder(MultilayerNetwork network)
        {
            _network = network;
        }
        
        public async Task<WeightedGraph<string, double>> BuildAsync(
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
            var numLayers = _network.Layers.Count();
            var mSize = _network.Layers.Max(x => x.Size);
            var unitW = width / numLayers;
            var unitH = height / mSize;
            var maxWeight = AllNeurons().Max(n => n.Export().Sum);

            foreach (var layer in _network.Layers.Reverse())
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

        IEnumerable<INeuron> AllNeurons()
        {
            foreach (var layer in _network.Layers)
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
