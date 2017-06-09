using LinqInfer.Maths.Geometry;
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
        /// <param name="bounds">The maximum spatial bounds of the graph</param>
        /// <param name="origin">The point origin of the graph</param>
        /// <param name="store">An optional storage mechanism</param>
        /// <returns></returns>
        Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            Point3D? bounds = null,
            Point3D origin = default(Point3D),
            IWeightedGraphStore<string, double> store = null);
    }
}