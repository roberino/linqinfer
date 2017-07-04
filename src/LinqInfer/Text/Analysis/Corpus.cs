using LinqInfer.Data;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public sealed class Corpus : IBinaryPersistable
    {
        private readonly IList<IToken> _tokens;

        public Corpus()
        {
            _tokens = new List<IToken>();
        }

        public Corpus(IEnumerable<IToken> tokens)
        {
            _tokens = tokens.ToList();
        }

        public void Append(IToken token)
        {
            _tokens.Add(token);
        }

        public IEnumerable<IToken> Words
        {
            get
            {
                return _tokens.Where(t => t.Type == TokenType.Word);
            }
        }

        public IEnumerable<IToken> WordsAndSymbols
        {
            get
            {
                return _tokens.Where(t => t.Type == TokenType.Word || t.Type == TokenType.Symbol);
            }
        }

        public Task<WeightedGraph<string, double>> ExportWordGraph(string word, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null)
        {
            return ExportWordGraph(t => string.Equals(t.Text, word, StringComparison.OrdinalIgnoreCase), maxFollowingConnections, store);
        }

        public async Task<WeightedGraph<string, double>> ExportWordGraph(Func<IToken, bool> targetTokenFunc, int maxFollowingConnections = 1, IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            foreach (var block in Blocks)
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

        public IEnumerable<IEnumerable<IToken>> Blocks
        {
            get
            {
                var currentBlock = new List<IToken>();
                int spaceCount = 0;
                int i = 0;
                TokenType lastType = TokenType.Null;

                foreach(var word in _tokens)
                {
                    if (word.Type == TokenType.Word || word.Type == TokenType.Number)
                    {
                        currentBlock.Add(new Token(word.Text, i++, TokenType.Word));
                        spaceCount = 0;
                    }
                    else
                    {
                        if (word.Type == TokenType.SentenceEnd || spaceCount > 1 || ((lastType == TokenType.Word || lastType == TokenType.Number) && word.Type == TokenType.Symbol && (word.Text == ";" || word.Text == ",")))
                        {
                            if (currentBlock.Any())
                            {
                                yield return currentBlock.ToList();

                                currentBlock.Clear();
                            }
                            spaceCount = 0;
                        }
                        else
                        {
                            if(word.Type == TokenType.Space)
                            {
                                spaceCount++;
                            }
                        }
                    }

                    lastType = word.Type;
                }

                if (currentBlock.Any()) yield return currentBlock.ToList();
            }
        }

        public void Load(Stream input)
        {
            using (var reader = new StreamReader(input))
            {
                int i = 0;
                while (true)
                {
                    var next = reader.ReadLine();

                    if (next == null) break;

                    var type = next == "." ? TokenType.SentenceEnd : TokenType.Word;

                    _tokens.Add(new Token(next, i++, type));
                }
            }
        }

        public void Save(Stream output)
        {
            using (var writer = new StreamWriter(output))
            {
                foreach (var block in Blocks)
                {
                    foreach (var word in block)
                    {
                        writer.WriteLine(word.Text);
                    }

                    writer.WriteLine(".");
                }
            }
        }
    }
}