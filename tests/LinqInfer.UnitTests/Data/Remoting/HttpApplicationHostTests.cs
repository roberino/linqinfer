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
        [Category("BuildOmit")]
        public async Task SendBasicRequest_RespondsAsExpected()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.Write("hi");

                    return Task.FromResult(0);
                }, OwinPipelineStage.PreHandlerExecute);

                host.Start();

                var text = await InvokeUrl(new Uri("http://localhost:9032/"));

                Assert.That(text, Is.EqualTo("hi"));
            }
        }

        [Test]
        [Category("BuildOmit")]
        public async Task Send_UsingRoutingHandler()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                var routingHandler = new RoutingHandler();

                host.AddComponent(routingHandler.CreateApplicationDelegate());

                routingHandler.AddRoute(new UriRoute(new Uri(Uri.UriSchemeHttp + Uri.SchemeDelimiter + "localhost:9032"), "/route/{param1}"), c =>
                {
                    c.Response.CreateTextResponse().Write("Hi " + c["route.param1"]);

                    return Task.FromResult(0);
                });

                host.Start();

                var text = await InvokeUrl(new Uri("http://localhost:9032/route/123"));

                Assert.That(text, Is.EqualTo("Hi 123"));
            }
        }

        [Test]
        [Category("BuildOmit")]
        public async Task Send_MultipleComponents_RespondsAsExpected()
        {
            using (var host = new HttpApplicationHost("123", 9032))
            {
                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.Write("hi");

                    return Task.FromResult(0);
                }, OwinPipelineStage.PreHandlerExecute);

                host.AddComponent(c =>
                {
                    var writer = c.Response.CreateTextResponse();

                    writer.Write(" there");

                    return Task.FromResult(0);
                }, OwinPipelineStage.PostHandlerExecute);

                host.Start();

                var text = await InvokeUrl(new Uri("http://localhost:9032/"));

                Assert.That(text, Is.EqualTo("hi there"));
            }
        }

        private async Task<string> InvokeUrl(Uri url)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = url;

                var response = await client.SendAsync(new HttpRequestMessage()
                {
                    RequestUri = client.BaseAddress
                });

                foreach (var s in response.Headers)
                {
                    Console.WriteLine(s);
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
