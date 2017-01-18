using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;
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

                //Thread.Sleep(120000);

                api.Stop();

                Assert.That(lastStatus, Is.EqualTo(ServerStatus.Stopped));
                Assert.That(api.Status, Is.EqualTo(ServerStatus.Stopped));
            }
        }
    }
}