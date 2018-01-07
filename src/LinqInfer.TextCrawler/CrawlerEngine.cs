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
        public async Task<IImportableExportableSemanticSet> ExtractVocabulary(Uri uri, CancellationToken cancellationToken)
        {
            var httpServices = new HttpDocumentServices();

            var corpus = httpServices.CreateVirtualCorpus(uri);

            return await corpus.ExtractKeyTermsAsync(cancellationToken);
        }

        public IAsyncPipe<SyntacticContext> CreatePipe(Uri uri, CancellationToken cancellationToken, ISemanticSet vocabulary)
        {
            var httpServices = new HttpDocumentServices();

            var corpus = httpServices.CreateVirtualCorpus(uri);

            var vocab = vocabulary ?? new EnglishDictionary();

            var cbow = corpus.CreateAsyncContinuousBagOfWords(vocab);

            var pipe = cbow.GetEnumerator().CreatePipe();

            pipe.Disposing += (s, e) => httpServices.Dispose();

            return pipe;
        }
    }
}