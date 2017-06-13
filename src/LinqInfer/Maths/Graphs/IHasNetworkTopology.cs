using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    /// <summary>
    /// Implemented by objects while can be represented as a network
    /// </summary>
    public interface IHasNetworkTopology
    {
        /// <summary>
        /// Exports the topology of the network as a <see cref="WeightedGraph{T, C}"/>, containing 
        /// the component elements of the network.
        /// </summary>
        /// <param name="visualSettings">Specifies the visual settings (inc. origin, bounds and colour palette) which are used
        /// when exporting the network into a graph.</param>
        /// <param name="store">An optional storage mechanism</param>
        /// <returns></returns>
        Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null);
    }
}