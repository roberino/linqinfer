using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public class HttpDocumentServices : IDisposable
    {
        public HttpDocumentServices(ITokeniser tokeniser = null,
            TextMimeType mimeType = TextMimeType.Default,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null) : this(new HttpBasicClient(), tokeniser, mimeType, nodeFilter, linkExtractor)
        {
        }

        public HttpDocumentServices(
            IHttpClient httpClient,
            ITokeniser tokeniser = null,
            TextMimeType mimeType = TextMimeType.Default,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null)
        {
            DocumentClient = new HttpDocumentClient(
                httpClient,
                tokeniser,
                mimeType,
                nodeFilter,
                linkExtractor
                );

            DocumentCrawler = new HttpDocumentCrawler(DocumentClient);
        }

        public HttpDocumentClient DocumentClient { get; }

        internal HttpDocumentCrawler DocumentCrawler { get; }

        public IAsyncEnumerator<HttpDocument> CreateDocumentSource(Uri rootUri, HttpDocumentCrawlerOptions options = null)
        {
            var batchLoader = new HttpDocumentSource(DocumentClient, rootUri, options ?? new HttpDocumentCrawlerOptions());

            return batchLoader.GetAsyncSource();
        }

        public void Dispose()
        {
            DocumentClient.Dispose();
        }
    }
}