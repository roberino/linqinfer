using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using LinqInfer.Text.Analysis;
using LinqInfer.Text.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Http
{
    public static class CrawlerExtensions
    {
        public static async Task<IDocumentIndex> CreateIndexAsync(this Uri uri, CancellationToken cancellationToken, Action<HttpDocumentCrawlerOptions> crawlerConfig = null)
        {
            using (var httpServices = new HttpDocumentServices())
            {
                var documentSource = CreateHttpEnumerator(httpServices, uri, crawlerConfig);

                var pipe = documentSource.CreatePipe();

                var index = pipe.AttachIndex();

                await pipe.RunAsync(cancellationToken);

                return index;
            }
        }

        public static IAsyncPipe<BiGram> CreatePipe(this Uri uri, CancellationToken cancellationToken, ISemanticSet vocabulary, Action<HttpDocumentCrawlerOptions> crawlerConfig = null)
        {
            var httpServices = new HttpDocumentServices();

            var settings = new HttpDocumentCrawlerOptions();

            crawlerConfig?.Invoke(settings);

            var corpus = httpServices.CreateDocumentSource(uri, settings).CreateVirtualCorpus();

            var vocab = vocabulary ?? new EnglishDictionary();

            var cbow = corpus.CreateAsyncContinuousBagOfWords(vocab);

            var pipe = cbow.GetBiGramSource().CreatePipe();
            
            pipe.Disposing += (s, e) => httpServices.Dispose();

            return pipe;
        }

        public static async Task<LabelledMatrix<string>> ExtractVectorsAsync(
            this Uri uri, CancellationToken cancellationToken, Action<HttpDocumentCrawlerOptions> crawlerConfig, params string[] vocabulary)
        {
            var vocabset = new SemanticSet(new HashSet<string>(vocabulary));

            return await ExtractVectorsAsync(uri, cancellationToken, crawlerConfig, vocabset);
        }

        public static async Task<LabelledMatrix<string>> ExtractVectorsAsync(this Uri uri,
            CancellationToken cancellationToken, Action<HttpDocumentCrawlerOptions> crawlerConfig = null,
            ISemanticSet vocabulary = null)
        {
            return (await ExtractVectorsInternalAsync(uri, cancellationToken, crawlerConfig, vocabulary)).Vectors;
        }

        static async Task<VectorExtractionResult> ExtractVectorsInternalAsync(this Uri uri, CancellationToken cancellationToken, Action<HttpDocumentCrawlerOptions> crawlerConfig = null, ISemanticSet vocabulary = null)
        {
            using (var httpServices = new HttpDocumentServices())
            {
                var corpus = CreateHttpEnumerator(httpServices, uri, crawlerConfig).CreateVirtualCorpus();

                var ed = new EnglishDictionary();
                var vocab = vocabulary ?? ed;

                var cbow = corpus.CreateAsyncContinuousBagOfWords(vocab, ed);

                var trainingSet = cbow.AsBiGramAsyncTrainingSet();

                return await trainingSet.ExtractVectorsAsync(cancellationToken, 8);
            }
        }

        static IAsyncEnumerator<HttpDocument> CreateHttpEnumerator(HttpDocumentServices httpServices, Uri uri, Action<HttpDocumentCrawlerOptions> crawlerConfig)
        {
            var settings = new HttpDocumentCrawlerOptions();

            crawlerConfig?.Invoke(settings);

            return httpServices.CreateDocumentSource(uri, settings);
        }
    }
}