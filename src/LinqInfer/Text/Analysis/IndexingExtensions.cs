using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using LinqInfer.Text.Indexing;

namespace LinqInfer.Text.Analysis
{
    public static class IndexingExtensions
    {
        public static IImportableExportableSemanticSet ExtractKeyTerms(this ICorpus corpus, int maxNumberOfTerms = 256)
        {
            var index = new DocumentIndex();
            int i = 0;

            index.IndexDocuments(corpus
                .Blocks
                .Select(b => new TokenisedTextDocument((i++).ToString(), b)));

            return index.ExtractKeyTerms(maxNumberOfTerms);
        }

        public static async Task<IImportableExportableSemanticSet> ExtractAllTermsAsync(this ICorpus corpus, CancellationToken cancellationToken = default)
        {
            var set = await corpus.ReadBlocksAsync()
                .TransformEachBatch(b => b.Where(t => t.Type == TokenType.Word).ToList())
                .TransformEachItem(t => t.Text.ToLowerInvariant())
                .ToDistinctSetAsync(cancellationToken);

            return new SemanticSet(set);
        }
    }
}