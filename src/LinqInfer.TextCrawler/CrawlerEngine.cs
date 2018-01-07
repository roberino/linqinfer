using LinqInfer.Text.Http;
using LinqInfer.Text.Analysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Text;

namespace LinqInfer.TextCrawler
{
    public class CrawlerEngine
    {
        public Task Run(Uri uri, CancellationToken cancellationToken)
        {
            var httpServices = new HttpDocumentServices();

            var corpus = httpServices.CreateVirtualCorpus(uri);

            var engDict = new EnglishDictionary();

            var cbow = corpus.CreateAsyncContinuousBagOfWords(engDict);

            return Task.FromResult(0);
        }
    }
}
