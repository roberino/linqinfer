using LinqInfer.Data.Pipes;
using LinqInfer.Maths.Graphs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public static class CorpusGraphExtensions
    {
        public static Task<WeightedGraph<string, double>> ExportWordGraphAsync(this ICorpus corpus, string word, CancellationToken cancellationToken, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null)
        {
            return ExportWordGraphAsync(corpus, t => string.Equals(t.Text, word, StringComparison.OrdinalIgnoreCase), cancellationToken, maxFollowingConnections, store);
        }

        public static async Task<WeightedGraph<string, double>> ExportWordGraphAsync(this ICorpus corpus, Func<IToken, bool> targetTokenFunc, CancellationToken cancellationToken, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null, int maxVerticeCount = 1000)
        {
            var pipe = corpus.ReadBlocksAsync().CreatePipe();
            var builder = new GraphSink(targetTokenFunc, maxFollowingConnections, maxVerticeCount, store);

            var attachedBuilder = pipe.Attach(builder);

            await attachedBuilder.Pipe.RunAsync(cancellationToken);
            
            await attachedBuilder.Output.SaveAsync();

            return attachedBuilder.Output;
        }
    }
}