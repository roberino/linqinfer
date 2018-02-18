using LinqInfer.Data.Pipes;
using System.Collections.Generic;

namespace LinqInfer.Text.Analysis
{
    public interface ICorpus
    {
        IEnumerable<IToken> Words { get; }

        IEnumerable<IEnumerable<IToken>> Blocks { get; }

        IAsyncEnumerator<IToken> ReadBlocksAsync();
    }
}