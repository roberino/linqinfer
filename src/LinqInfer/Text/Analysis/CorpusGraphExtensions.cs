using LinqInfer.Maths.Graphs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public static class CorpusGraphExtensions
    {
        public static Task<WeightedGraph<string, double>> ExportWordGraph(this ICorpus corpus, string word, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null)
        {
            return ExportWordGraph(corpus, t => string.Equals(t.Text, word, StringComparison.OrdinalIgnoreCase), maxFollowingConnections, store);
        }

        public static async Task<WeightedGraph<string, double>> ExportWordGraph(this ICorpus corpus, Func<IToken, bool> targetTokenFunc, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            foreach (var block in corpus.Blocks)
            {
                int i = -1;
                IToken last = null;
                WeightedGraphNode<string, double> currentNode = null;

                foreach (var token in block.Where(t => t.Type == TokenType.Word))
                {
                    if (i > -1)
                    {
                        currentNode = await currentNode.ConnectToOrModifyWeightAsync(token.Text.ToLower(), 1, x => x++);

                        i++;

                        if (i == maxFollowingConnections)
                        {
                            i = -1;
                            currentNode = null;
                        }
                    }
                    else
                    {
                        if (targetTokenFunc(token))
                        {
                            i = 0;

                            if (last != null)
                            {
                                currentNode = await graph.FindOrCreateVertexAsync(last.Text.ToLower());
                                currentNode = await currentNode.ConnectToOrModifyWeightAsync(token.Text.ToLower(), 1, x => x++);
                            }
                            else
                            {
                                currentNode = await graph.FindOrCreateVertexAsync(token.Text.ToLower());
                            }

                            var attribs = await currentNode.GetAttributesAsync();

                            attribs["IsTarget"] = true;
                        }
                    }

                    last = token;
                }
            }

            await graph.SaveAsync();

            return graph;
        }
    }
}