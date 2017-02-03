using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.MicroServices;
using Microsoft.Owin.Hosting;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Owin.Tests
{
    [TestFixture]
    public class ExtensionsTests
    {
        [Test]
        public async Task CreateHttpApi_Bind_AndTest_ReturnsValidObj()
        {
            using (var api = new Uri("http://localhost:8023").CreateHttpApi())
            {
                api.Bind("/{x}").To(1, x =>
                {
                    return Task.FromResult(x * 2);
                });

                var result = await api.TestRoute<int>(new Uri("http://localhost:8023/5"));

                Assert.That(result, Is.EqualTo(10));
            }
        }

        [Test]
        public async Task CreateHttpApi_UsingIAppBuilder()
        {
            var serverUri = new Uri("http://localhost:8023");

            using (WebApp.Start(serverUri.ToString(), a =>
            {
                a.CreateHttpApi(serverUri).Bind("/{x}").To(1, x =>
                {
                    return Task.FromResult(x * 2);
                });
            }))
            {
                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync(new Uri(serverUri, "/16"));

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That(int.Parse(text), Is.EqualTo(32));
                }
            }
        }

        [Test]
        public async Task CreateHttpApi_UsingOwinApiBuilder()
        {
            var serverUri = new Uri("http://localhost:8023");

            var middleware = serverUri.CreateHttpApiBuilder();

            middleware.Bind("/{x}").To(1, x => Task.FromResult(x * 2));

            using (WebApp.Start(serverUri.ToString(), a =>
            {
                a.Run(middleware);
            }))
            {
                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync(new Uri(serverUri, "/16"));

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That(int.Parse(text), Is.EqualTo(32));
                }
            }
        }

        [Test]
        public async Task CreateHttpApi_Bind_AndInvoke_ReturnsValidResponse()
        {
            var serverUri = new Uri("http://localhost:8023");

            using (var api = serverUri.CreateHttpApi())
            {
                api.Bind("/{x}").To(1, x =>
                {
                    return Task.FromResult(x * 2);
                });

                api.Start();

                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync(new Uri(serverUri, "/16"));

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That(int.Parse(text), Is.EqualTo(32));
                }
            }
        }

        [Test]
        public async Task CreateOwinApplication_BufferedResponse_WriteAndCloneResponse()
        {
            var serverUri = new Uri("http://localhost:8023");
            IOwinContext clonedContext = null;

            using (var app = serverUri.CreateOwinApplication(true))
            {
                app.AddComponent(c =>
                {
                    c.Response.CreateTextResponse().Write("Hello");

                    clonedContext = c.Clone(true);

                    return Task.FromResult(1);
                });

                app.Start();

                using (var client = new HttpClient())
                {
                    var res = await client.GetAsync(serverUri);

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That(text, Is.EqualTo("Hello"));
                }

                Assert.That(clonedContext, Is.Not.Null);
            }

            Assert.That(clonedContext.Request.Header.ContentLength, Is.EqualTo(0));
            Assert.That(clonedContext.Request.Header.Headers.Count, Is.GreaterThan(0));
            Assert.That(clonedContext.Request.Header.HttpVerb, Is.EqualTo("GET"));
            Assert.That(clonedContext.Request.Header.Path, Is.EqualTo("/"));
            Assert.That(clonedContext.Request.Content.Position, Is.EqualTo(0));
            Assert.That(clonedContext.Response.Content.Position, Is.EqualTo(8));
        }

        [Test]
        public void CreateHttpApi_StartServer_StopServer_StatusIsCorrect()
        {
            using (var api = new Uri("http://localhost:8023").CreateHttpApi())
            {
                api.Bind("/{x}").To(1, x =>
                {
                    return Task.FromResult(x * 2);
                });

                api.Bind("/{x}", Verb.Post).To(1, x =>
                {
                    return Task.FromResult(x * 3);
                });

                ServerStatus lastStatus = ServerStatus.Unknown;

                api.StatusChanged += (s, e) =>
                {
                    lastStatus = e.Value;
                };

                api.Start();

                Assert.That(lastStatus, Is.EqualTo(ServerStatus.Running));
                Assert.That(api.Status, Is.EqualTo(ServerStatus.Running));

                Thread.Sleep(30000);

                api.Stop();

                Assert.That(lastStatus, Is.EqualTo(ServerStatus.Stopped));
                Assert.That(api.Status, Is.EqualTo(ServerStatus.Stopped));
            }
        }
    }
}