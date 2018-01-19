using LinqInfer.Data.Pipes;
using LinqInfer.Text.Analysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Http
{
    public static class HttpDocumentSourceExtensions
    {
        /// <summary>
        /// Creates a dynamic corpus of text
        /// sourced from http documents
        /// </summary>
        public static ICorpus CreateVirtualCorpus(this IAsyncEnumerator<HttpDocument> httpDocumentSource)
        {
            var tokenSource = httpDocumentSource.TransformEachBatch(b =>
            {
                return b.SelectMany(d => d.Tokens).ToList();
            });

            return new VirtualCorpus(tokenSource.Items);
        }

        /// <summary>
        /// Pushes the document source into a corpus
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static async Task<ICorpus> CreateCorpusAsync(this IAsyncEnumerator<HttpDocument> httpDocumentSource, int maxCapacity = 1000)
        {
            return await RunAsync(httpDocumentSource, new CorpusSink(maxCapacity));
        }

        /// <summary>
        /// Pushes the document source into a corpus
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static ICorpus AttachCorpusAsync(this IAsyncPipe<HttpDocument> httpDocumentPipeline, int maxCapacity = 1000)
        {
            var corpusSink = new CorpusSink(maxCapacity);

            httpDocumentPipeline.RegisterSinks(corpusSink);

            return corpusSink.Output;
        }

        /// <summary>
        /// Creates an index from a document source
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static async Task<IDocumentIndex> CreateIndexAsync(this IAsyncEnumerator<HttpDocument> httpDocumentSource, int maxCapacity = 1000)
        {
            return await RunAsync(httpDocumentSource, new IndexSink(maxCapacity));
        }

        /// <summary>
        /// Attaches and index to the pipeline
        /// which will be populated when the pipeline runs
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static IDocumentIndex AttachIndex(this IAsyncPipe<HttpDocument> documentPipeline, int maxCapacity = 1000)
        {
            var indexSink = new IndexSink(maxCapacity);

            documentPipeline.RegisterSinks(indexSink);

            return indexSink.Output;
        }

        private static async Task<T> RunAsync<T>(this IAsyncEnumerator<HttpDocument> httpDocumentSource, IBuilderSink<HttpDocument, T> sink)
        {
            var pipe = httpDocumentSource.CreatePipe().RegisterSinks(sink);

            await pipe.RunAsync(CancellationToken.None);

            return sink.Output;
        }
    }
}