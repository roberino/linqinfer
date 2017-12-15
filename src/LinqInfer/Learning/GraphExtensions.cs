using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    public static class GraphExtensions
    {
        public static Task<WeightedGraph<DataLabel<TInput>, double>> CreateBinaryGraphAsync<TInput>(this IAsyncFeatureProcessingPipeline<TInput> pipeline, int estimatedSampleSize, CancellationToken? cancellationToken = null, IWeightedGraphStore<DataLabel<TInput>, double> weightedGraphStore = null)
            where TInput : class
        {
            var builder = new HeirarchicalGraphBuilder<TInput>(weightedGraphStore ?? new WeightedGraphInMemoryStore<DataLabel<TInput>, double>(), estimatedSampleSize);

            return builder.CreateBinaryGraph(pipeline, cancellationToken.GetValueOrDefault(CancellationToken.None));
        }
    }
}