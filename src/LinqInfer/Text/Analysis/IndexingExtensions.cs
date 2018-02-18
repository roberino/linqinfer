using System.Linq;

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
    }
}