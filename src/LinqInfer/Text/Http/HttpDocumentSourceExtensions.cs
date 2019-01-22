using LinqInfer.Data.Pipes;
using LinqInfer.Text.Analysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Text.Indexing;

namespace LinqInfer.Text.Http
{
    public static class HttpDocumentSourceExtensions
    {
        /// <summary>
        /// Creates a document source from a URL
        /// </summary>
        public static IAsyncSource<HttpDocument> CreateSource(this Uri rootUrl, HttpDocumentCrawlerOptions crawlerOptions = null)
        {
            var services = new HttpDocumentServices();

            return services.CreateAutoDisposingDocumentSource(rootUrl, crawlerOptions);
        }

        /// <summary>
        /// Creates a dynamic corpus of text
        /// sourced from http documents
        /// </summary>
        public static ICorpus CreateVirtualCorpus(this IAsyncEnumerator<HttpDocument> httpDocumentSource)
        {
            var tokenSource = (AsyncEnumerator<IToken>)httpDocumentSource.TransformEachBatch(b =>
            {
                return b.SelectMany(d => d.Tokens).ToList();
            });

            return new VirtualCorpus(tokenSource.Items);
        }

        /// <summary>
        /// Pushes the document source into a corpus
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static async Task<ICorpus> CreateCorpusAsync(this IAsyncEnumerator<HttpDocument> httpDocumentSource, CancellationToken cancellationToken, int maxCapacity = 1000)
        {
            return await RunAsync(httpDocumentSource, new CorpusSink(maxCapacity), cancellationToken);
        }

        /// <summary>
        /// Pushes the document source into a corpus
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static ICorpus AttachCorpus(this IAsyncPipe<HttpDocument> httpDocumentPipeline, int maxCapacity = 1000)
        {
            var corpusSink = new CorpusSink(maxCapacity);

            httpDocumentPipeline.RegisterSinks(corpusSink);

            return corpusSink.Output;
        }

        /// <summary>
        /// Creates an index from a document source
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static async Task<IDocumentIndex> CreateIndexAsync(this IAsyncEnumerator<HttpDocument> httpDocumentSource, CancellationToken cancellationToken, int maxCapacity = 1000)
        {
            return await RunAsync(httpDocumentSource, new IndexSink(maxCapacity), cancellationToken);
        }

        /// <summary>
        /// Attaches and index to the pipeline
        /// which will be populated when the pipeline runs
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static IDocumentIndex AttachIndex(this IAsyncPipe<HttpDocument> documentPipeline, int maxCapacity = 1000)
        {
            var indexSink = new IndexSink(maxCapacity);

            var output = documentPipeline.Attach(indexSink);
            
            return output.Output;
        }

        static async Task<T> RunAsync<T>(this IAsyncEnumerator<HttpDocument> httpDocumentSource, IBuilderSink<HttpDocument, T> sink, CancellationToken cancellationToken)
        {
            var pipe = httpDocumentSource.CreatePipe().RegisterSinks(sink);

            await pipe.RunAsync(cancellationToken);

            return sink.Output;
        }
    }
}