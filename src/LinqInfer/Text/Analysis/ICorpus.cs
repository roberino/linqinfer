using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public interface ICorpus
    {
        IEnumerable<IToken> Words { get; }

        IEnumerable<IEnumerable<IToken>> Blocks { get; }

        IEnumerable<Task<IList<IToken>>> ReadBlocksAsync();
    }
}