using LinqInfer.Data.Pipes;
using LinqInfer.Text.Analysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Http
{
    public static class HttpDocumentExtensions
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
            var corpusSink = new CorpusSink(maxCapacity);

            return await RunAsync(httpDocumentSource, corpusSink);
        }

        /// <summary>
        /// Creates an index from a document source
        /// </summary>
        /// <param name="maxCapacity">The max number of docs to be indexed</param>
        public static async Task<IDocumentIndex> CreateIndexAsync(this IAsyncEnumerator<HttpDocument> httpDocumentSource, int maxCapacity = 1000)
        {
            var indexSink = new IndexSink(maxCapacity);

            return await RunAsync(httpDocumentSource, indexSink);
        }

        private static async Task<T> RunAsync<T>(this IAsyncEnumerator<HttpDocument> httpDocumentSource, IBuilder<HttpDocument, T> sink)
        {
            var pipe = httpDocumentSource.CreatePipe().RegisterSinks(sink);

            await pipe.RunAsync(CancellationToken.None);

            return await sink.BuildAsync();
        }
    }
}