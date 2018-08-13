using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Maths.Graphs;
using LinqInfer.Utility;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    class GraphSink : IBuilderSink<IToken, WeightedGraph<string, double>>
    {
        public GraphSink(
            Func<IToken, bool> targetTokenFunc,
            int maxFollowingConnections = 1,
            int maxVerticies = 1000,
            IWeightedGraphStore<string, double> store = null)
        {
            Output = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);
            TokenFilter = ArgAssert.AssertNonNull(targetTokenFunc, nameof(targetTokenFunc));
            MaxFollowingConnections = ArgAssert.AssertGreaterThanZero(maxFollowingConnections, nameof(maxFollowingConnections));
            MaxVerticies = ArgAssert.AssertGreaterThanZero(maxVerticies, nameof(maxVerticies));
        }

        public Func<IToken, bool> TokenFilter { get; }

        public int MaxVerticies { get; }

        public int MaxFollowingConnections { get; }

        public WeightedGraph<string, double> Output { get; }

        public bool CanReceive => Output.VerticeCount <= MaxVerticies;

        public async Task ReceiveAsync(IBatch<IToken> dataBatch, CancellationToken cancellationToken)
        {
            int i = -1;
            IToken last = null;
            WeightedGraphNode<string, double> currentNode = null;

            foreach (var token in dataBatch.Items.Where(t => t.Type == TokenType.Word))
            {
                if (i > -1)
                {
                    currentNode = await currentNode.ConnectToOrModifyWeightAsync(token.Text.ToLower(), 1, x => x++);

                    i++;

                    if (i == MaxFollowingConnections)
                    {
                        i = -1;
                        currentNode = null;
                    }
                }
                else
                {
                    if (TokenFilter(token))
                    {
                        i = 0;

                        if (last != null)
                        {
                            currentNode = await Output.FindOrCreateVertexAsync(last.Text.ToLower());
                            currentNode = await currentNode.ConnectToOrModifyWeightAsync(token.Text.ToLower(), 1, x => x++);
                        }
                        else
                        {
                            currentNode = await Output.FindOrCreateVertexAsync(token.Text.ToLower());
                        }

                        var attribs = await currentNode.GetAttributesAsync();

                        attribs["IsTarget"] = true;
                    }
                }

                last = token;
            }
        }
    }
}