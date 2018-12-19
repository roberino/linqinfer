using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    class SemanticSetBuilder
    {
        readonly SemanticSetSink _sink;

        public SemanticSetBuilder()
        {
            _sink = new SemanticSetSink();
        }

        public async Task AddAsync(IAsyncCorpus corpus, CancellationToken cancellationToken)
        {
            var pipe = corpus.ReadBlocksAsync().CreatePipe();
            var sink = new SemanticSetSink();

            pipe.RegisterSinks(_sink);

            await pipe.RunAsync(cancellationToken);
        }

        public IImportableExportableSemanticSet Build()
        {
            return new SemanticSet(new HashSet<string>(_sink.Words.OrderByDescending(w => w.Value).Select(w => w.Key)));
        }

        class SemanticSetSink : IAsyncSink<IToken>
        {
            public SemanticSetSink()
            {
                Words = new Dictionary<string, int>();
            }

            public IDictionary<string, int> Words { get; }

            public bool CanReceive => true;

            public Task ReceiveAsync(IBatch<IToken> dataBatch, CancellationToken cancellationToken)
            {
                string word;

                foreach(var item in dataBatch.Items.Where(w => w.Type == TokenType.Word))
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    word = item.Text.ToLowerInvariant();

                    if (!Words.TryGetValue(word, out int c))
                    {
                        c = 0;
                    }

                    Words[word] = c + 1;
                }

                return Task.FromResult(0);
            }
        }
    }
}