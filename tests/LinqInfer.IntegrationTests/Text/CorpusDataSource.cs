using LinqInfer.Text;
using LinqInfer.Text.Analysis;

namespace LinqInfer.IntegrationTests.Text
{
    internal static class CorpusDataSource
    {
        public static ICorpus GetCorpus()
        {
            using (var corpusStream = TestFixtureBase.GetResource("shakespeare.txt"))
            {
                return new Corpus(corpusStream.Tokenise());
            }
        }
    }
}