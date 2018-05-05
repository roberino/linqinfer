using LinqInfer.Data;
using LinqInfer.Microservices.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using LinqInfer.Data.Storage;
using Xunit;

namespace LinqInfer.Microservices.Tests
{
    public class TestServicesIntegrationTests : IDisposable
    {
        private readonly TestServer _server;
        private readonly InMemoryFileStorage _fileStore;

        public TestServicesIntegrationTests()
        {
            var builder = new WebHostBuilder();

            _fileStore = new InMemoryFileStorage();

            builder.Configure(app =>
            {
                app.UseTextServices(_fileStore);
            });

            _server = new TestServer(builder);
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        [Fact]
        public async Task CreateIndexAndAddDoc()
        {
            using (var client = _server.CreateClient())
            {
                await CreateIndex(client);
                await CreateDocument(client);
            }
        }

        private async Task CreateIndex(HttpClient client)
        {
            var urlPath = "/text/indexes/index1";
            var request = new StringContent("");

            using (var response = await client.PostAsync(urlPath, request))
            {
                var content = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<dynamic>(content);

                Assert.True(((string)data.indexName).Equals("index1"));
            }
        }

        private async Task CreateDocument(HttpClient client)
        {
            var urlPath = "/text/indexes/index1/documents/id1";
            var request = CreateContent(new
            {
                text = "a b c"
            });

            using (var response = await client.PostAsync(urlPath, request))
            {
                var content = await response.Content.ReadAsStringAsync();

                var data = JsonConvert.DeserializeObject<dynamic>(content);

                Assert.True(((string)data.indexName).Equals("index1"));
            }
        }

        private HttpContent CreateContent<T>(T obj)
        {
            var json = JsonConvert.SerializeObject(obj);

            return new StringContent(json);
        }
    }
}