using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using System.Linq;

namespace LinqInfer.IntegrationTests.Text
{
    static class CorpusDataSource
    {
        public static ICorpus GetCorpus(int? numberOfWords = null)
        {
            using (var corpusStream = TestFixtureBase.GetResource("shakespeare.txt"))
            {
                if (numberOfWords.HasValue)
                {
                    return new Corpus(corpusStream.Tokenise().Take(numberOfWords.Value));
                }

                return new Corpus(corpusStream.Tokenise());
            }
        }
    }
}