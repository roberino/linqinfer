using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpBasicClient : IDisposable, IHttpClient
    {
        private readonly HttpClient _client;

        public HttpBasicClient()
        {
            _client = new HttpClient();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        public async Task<TcpResponse> GetAsync(Uri url)
        {
            var response = await _client.GetAsync(url);
            var stream = await response.Content.ReadAsStreamAsync();

            return new TcpResponse(new TcpResponseHeader(
                () => response.Content.Headers.ContentLength.GetValueOrDefault(),
                response.Headers.Concat(response.Content.Headers).ToDictionary(h => h.Key, h => h.Value.ToArray())), stream);
        }
   }
}