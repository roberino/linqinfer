using LinqInfer.Microservices.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace LinqInfer.Microservices.Tests
{
    public class TestServicesIntegrationTests : IDisposable
    {
        private readonly TestServer _server;

        public TestServicesIntegrationTests()
        {
            var builder = new WebHostBuilder();

            builder.Configure(app =>
            {
                app.UseTextServices();
            });

            _server = new TestServer(builder);
        }

        public void Dispose()
        {
            _server.Dispose();
        }

        [Fact]
        public async Task CreateIndex()
        {
            using(var client = _server.CreateClient())
            {
                var urlPath = "/text/indexes/index1";
                var content = new StringContent("");

                var response = await client.PostAsync(urlPath, content);

                var data = JsonConvert.DeserializeObject<dynamic>(await response.RequestMessage.Content.ReadAsStringAsync());
            }
        }
    }
}