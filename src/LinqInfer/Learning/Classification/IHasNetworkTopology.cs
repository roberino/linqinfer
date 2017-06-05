using System.Threading.Tasks;
using LinqInfer.Maths.Graphs;

namespace LinqInfer.Learning.Classification
{
    public interface IHasNetworkTopology
    {
        Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(IWeightedGraphStore<string, double> store = null);
    }
}