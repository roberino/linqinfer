using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class HttpApplicationHostTests
    {
        [Test]
        public void AddComponent()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.WriteLine("hi");

                    return Task.FromResult(0);
                });
            }
        }
        [Test]
        public async void SendBasicRequest_RespondsAsExpected()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.WriteLine("hi");

                    return Task.FromResult(0);
                });

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:9032/");

                    var response = await client.SendAsync(new HttpRequestMessage()
                    {
                        RequestUri = client.BaseAddress
                    });
                }
            }
        }
    }
}
