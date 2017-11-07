using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public interface ICorpus
    {
        IEnumerable<Task<IList<IToken>>> ReadBlocksAsync();

        IEnumerable<IEnumerable<IToken>> Blocks { get; }
        IEnumerable<IToken> Words { get; }
        IEnumerable<IToken> WordsAndSymbols { get; }
    }
}