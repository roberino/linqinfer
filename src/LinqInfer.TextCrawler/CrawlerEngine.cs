using LinqInfer.Text.Http;
using LinqInfer.Text.Analysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Text;
using LinqInfer.Data.Pipes;

namespace LinqInfer.TextCrawler
{
    public class CrawlerEngine
    {
        public async Task<IDocumentIndex> CreateIndexAsync(Uri uri, CancellationToken cancellationToken)
        {
            var httpServices = new HttpDocumentServices();

            var documentSource = httpServices.CreateDocumentSource(uri);

            var index = await documentSource.CreateIndexAsync();

            var corpus = await documentSource.CreateCorpusAsync(1000);

            var trainingSet = corpus.CreateContinuousBagOfWordsAsyncTrainingSet(index.ExtractKeyTerms(500));

            return index;
        }

        public IAsyncPipe<SyntacticContext> CreatePipe(Uri uri, CancellationToken cancellationToken, ISemanticSet vocabulary)
        {
            var httpServices = new HttpDocumentServices();

            var corpus = httpServices.CreateDocumentSource(uri).CreateVirtualCorpus();

            var vocab = vocabulary ?? new EnglishDictionary();

            var cbow = corpus.CreateAsyncContinuousBagOfWords(vocab);

            var pipe = cbow.GetEnumerator().CreatePipe();

            pipe.Disposing += (s, e) => httpServices.Dispose();

            return pipe;
        }
    }
}