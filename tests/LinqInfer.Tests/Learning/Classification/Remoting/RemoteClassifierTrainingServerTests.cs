using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.Remoting;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning.Classification.Remoting
{
    [TestFixture]
    public class RemoteClassifierTrainingServerTests
    {
        [Test]
        public async Task Send()
        {
            var endpoint = new Uri("tcp://localhost:9210");

            var data = Functions.NormalRandomDataset(3, 10).Select(x => new
            {
                x = x,
                y = Math.Log(x)
            }).AsQueryable();

            using (var blobs = new InMemoryBlobStore())
            using (var server = new RemoteClassifierTrainingServer(endpoint, blobs))
            using (var client = new RemoteClassifierTrainingClient(endpoint))
            {
                server.Start();

                var pipeline = data.CreatePipeline();

                var results = await client.Send(pipeline, x => x.x > 10 ? 'a' : 'b');
                
                var nn = new MultilayerNetwork(new NetworkParameters(1, 1));

                blobs.Restore(results.Key, nn);
            }
        }
    }
}