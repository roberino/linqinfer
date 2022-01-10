using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    class VirtualCorpus : ICorpus
    {
        readonly IEnumerable<Task<IList<IToken>>> _tokenEnumerator;

        public VirtualCorpus(IEnumerable<Task<IList<IToken>>> tokenEnumerator)
        {
            _tokenEnumerator = tokenEnumerator ?? throw new ArgumentNullException(nameof(tokenEnumerator));
        }

        public IEnumerable<IToken> Words => Blocks.SelectMany(b => b.Where(t => t.Type == TokenType.Word));

        public IEnumerable<IEnumerable<IToken>> Blocks => ReadBlocksAsyncInternal().Select(b => b.Result);

        public Data.Pipes.IAsyncEnumerator<IToken> ReadBlocksAsync()
        {
            return ReadBlocksAsyncInternal().AsAsyncEnumerator();
        }

        IEnumerable<Task<IList<IToken>>> ReadBlocksAsyncInternal()
        {
            foreach (var batchTask in _tokenEnumerator)
            {
                var f = new Func<Task<IEnumerable<IEnumerable<IToken>>>>(async () =>
                {
                    var batchValues = await batchTask;

                    return new BlockReader().ReadBlocks(batchValues);
                });

                var batch = f();

                yield return batch
                    .ContinueWith(x =>
                    {
                        return (IList<IToken>)(x.Result.FirstOrDefault() ?? new List<IToken>()).ToList();
                    });

                foreach (var item in batch.Result.Skip(1))
                {
                    yield return Task.FromResult<IList<IToken>>(item.ToList());
                }
            }
        }
    }
}