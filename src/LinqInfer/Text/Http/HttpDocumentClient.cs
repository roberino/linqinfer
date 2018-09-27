using LinqInfer.Data.Remoting;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public sealed class HttpDocumentClient : IDisposable
    {
        const int MaxVisited = 350;

        readonly IHttpClient _client;
        readonly IContentReader _contentReader;

        internal HttpDocumentClient(
            IHttpClient httpClient,
            IContentReader contentReader)
        {
            _client = httpClient;
            _contentReader = contentReader;
        }

        public Task<HttpDocument> GetDocumentAsync(Uri rootUri, Func<XElement, XElement> targetElement = null)
        {
            var current = Read(rootUri, targetElement);

            return current;
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        async Task<HttpDocument> Read(Uri uri, Func<XElement, XElement> targetElement)
        {            
            var response = await _client.GetAsync(uri);

            var enc = response.Header.TextEncoding ?? Encoding.UTF8;
            
            using (response.Content)
            {
                return await _contentReader.ReadAsync(uri, response.Content, response.Header.Headers, response.Header.ContentMimeType, enc, targetElement);
            }
        }
    }
}