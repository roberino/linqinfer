using System.Threading.Tasks;
using LinqInfer.Maths.Graphs;

namespace LinqInfer.Learning.Classification
{
    public interface IHasNetworkTopology
    {
        Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            double width = 100,
            double height = 100, 
            IWeightedGraphStore<string, double> store = null);
    }
}