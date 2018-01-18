using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public class HttpDocumentServices : IDisposable
    {
        private readonly HttpDocumentClient _documentClient;

        public HttpDocumentServices(
            ITokeniser tokeniser = null,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null) : this(new HttpBasicClient(), tokeniser, nodeFilter, linkExtractor)
        {
        }

        public HttpDocumentServices(
            IHttpClient httpClient,
            ITokeniser tokeniser = null,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null) : this(httpClient, new DefaultContentReader(tokeniser, nodeFilter, linkExtractor))
        {
        }

        public HttpDocumentServices(
            IHttpClient httpClient,
            IContentReader contentReader)
        {
            _documentClient = new HttpDocumentClient(httpClient, contentReader);
        }

        public Task<HttpDocument> GetDocumentAsync(Uri url)
        {
            return _documentClient.GetDocumentAsync(url);
        }

        public IAsyncEnumerator<HttpDocument> CreateDocumentSource(Uri rootUri, HttpDocumentCrawlerOptions options = null)
        {
            var batchLoader = new HttpDocumentSource(_documentClient, rootUri, options ?? new HttpDocumentCrawlerOptions());

            return batchLoader.GetAsyncSource();
        }

        public void Dispose()
        {
            _documentClient.Dispose();
        }
    }
}