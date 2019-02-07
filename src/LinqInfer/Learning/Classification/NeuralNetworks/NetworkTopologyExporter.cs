using LinqInfer.Maths.Graphs;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class NetworkTopologyExporter
    {
        readonly MultilayerNetwork _network;

        public NetworkTopologyExporter(MultilayerNetwork network)
        {
            _network = network;
        }
        
        public async Task<WeightedGraph<string, double>> ExportAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);
            
            var vs = visualSettings ?? new VisualSettings();

            await ExportAsync(graph, vs, _network.RootModule, 1);

            await graph.SaveAsync();

            return graph;
        }

        async Task<WeightedGraphNode<string, double>> ExportAsync(WeightedGraph<string, double> graph, VisualSettings visualSettings, INetworkSignalFilter networkSignalFilter, int index)
        {
            var node = await graph.FindOrCreateVertexAsync(networkSignalFilter.ToString());
            
            var colour = visualSettings.Palette.GetColourByIndex(index);

            // await node.SetPositionAndSizeAsync(vs.Origin.X + width - unitW * l, vs.Origin.Y + unitH * i - offsetY, 0, Math.Min(unitH, unitW) / 2);
            
            await node.SetColourAsync(colour);

            foreach (var successor in ((NetworkModule)networkSignalFilter).Successors)
            {
                var successorNode = await ExportAsync(graph, visualSettings, successor, index++);

                await node.ConnectToAsync(successorNode, 1);
            }

            return node;
        }
    }
}
