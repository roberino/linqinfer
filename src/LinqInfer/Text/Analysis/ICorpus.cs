using LinqInfer.Data.Pipes;
using System.Collections.Generic;

namespace LinqInfer.Text.Analysis
{
    public interface IAsyncCorpus
    {
        ITransformingAsyncBatchSource<IToken> ReadBlocksAsync();
    }

    public interface ICorpus : IAsyncCorpus
    {
        IEnumerable<IToken> Words { get; }

        IEnumerable<IEnumerable<IToken>> Blocks { get; }
    }
}