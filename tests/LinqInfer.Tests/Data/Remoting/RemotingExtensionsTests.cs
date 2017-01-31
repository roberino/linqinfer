using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class RemotingExtensionsTests
    {
        public void CreateHttpApplication_ReturnsValidInstance()
        {
            var endpoint = new Uri("http://localhost:9101/my-service");

            var app = endpoint.CreateHttpApplication();

            Assert.That(app, Is.Not.Null);

            app.AddComponent(c => Task.FromResult(0), OwinPipelineStage.Authenticate);

            app.Dispose();
        }

        [Category("BuildOmit")]
        [Test]
        public async Task CreateRemoteService_SendTo()
        {
            var endpoint = new Uri("tcp://localhost:9101/my-service");

            var dataX0 = Functions.NormalRandomDataset(3, 10);

            var data = dataX0.Select(x => new
            {
                x = x,
                y = Math.Log(x)
            }).AsQueryable();

            var server = endpoint.CreateRemoteService((s, r) =>
            {
                foreach(var item in s.Vectors)
                {
                    Console.WriteLine(item);
                }

                return true;
            });

            await data
                .CreatePipeline()
                .SendTo(endpoint);

            server.Stop();

            Thread.Sleep(150);

            server.Dispose();
        }
    }
}