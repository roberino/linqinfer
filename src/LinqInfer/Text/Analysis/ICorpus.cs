using LinqInfer.Utility;
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