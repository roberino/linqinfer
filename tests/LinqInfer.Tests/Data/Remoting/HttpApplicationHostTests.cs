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
        public async Task SendBasicRequest_RespondsAsExpected()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.Write("hi");

                    return Task.FromResult(0);
                });

                host.Start();

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://localhost:9032/");

                    var response = await client.SendAsync(new HttpRequestMessage()
                    {
                        RequestUri = client.BaseAddress
                    });

                    var encoding = response.Headers.GetValues("Content-Type");

                    foreach(var s in encoding)
                    {
                        Console.WriteLine(s);
                    }

                    var text = await response.Content.ReadAsStringAsync();

                    Assert.That(text, Is.EqualTo("hi"));
                }
            }
        }
    }
}
