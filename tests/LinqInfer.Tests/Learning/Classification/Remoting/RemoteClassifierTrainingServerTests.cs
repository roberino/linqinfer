using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.Remoting;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning.Classification.Remoting
{
    [TestFixture]
    public class RemoteClassifierTrainingServerTests
    {
        [Test]
        public async Task RestoreClassifier_ReturnsValidObjectToClient()
        {
            var bytes = BitConverter.GetBytes(0);

            Assert.That(bytes.Length, Is.EqualTo(4));

            var endpoint = new Uri("tcp://localhost:9211");

            var data = Functions.NormalRandomDataset(3, 10).Select(x => new
            {
                x = x,
                y = Math.Log(x)
            }).AsQueryable();

            using (var blobs = new InMemoryBlobStore())
            using (var server = new RemoteClassifierTrainingServer(endpoint, blobs))
            using (var client = new RemoteClassifierTrainingClient(endpoint))
            {
                client.Timeout = 10000;

                server.Start();

                var trainingSet = data.CreatePipeline().AsTrainingSet(x => x.x > 10 ? 'a' : 'b');

                var keyAndclassifier = await client.CreateClassifier(trainingSet, true);

                var restoredClassifier = await client.RestoreClassifier(keyAndclassifier.Key, trainingSet.FeaturePipeline.FeatureExtractor, 'a');

                var results = restoredClassifier.Classify(new
                {
                    x = 7d,
                    y = 4.543d
                });
            }
        }

        [Test]
        public async Task CreateClassifier_SavesOutput()
        {
            var bytes = BitConverter.GetBytes(0);

            Assert.That(bytes.Length, Is.EqualTo(4));

            var endpoint = new Uri("tcp://localhost:9212");

            var data = Functions.NormalRandomDataset(3, 10).Select(x => new
            {
                x = x,
                y = Math.Log(x)
            }).AsQueryable();

            using (var blobs = new InMemoryBlobStore())
            using (var server = new RemoteClassifierTrainingServer(endpoint, blobs))
            using (var client = new RemoteClassifierTrainingClient(endpoint))
            {
#if DEBUGGING
                client.Timeout = 20000;
#endif
                server.Start();

                var trainingSet = data.CreatePipeline().AsTrainingSet(x => x.x > 10 ? 'a' : 'b');

                var keyAndclassifier = await client.CreateClassifier(trainingSet, true);

                // We should be able to restore the raw blob back into a network

                var nn = new MultilayerNetwork(new NetworkParameters(1, 1));

                blobs.Restore(new string(keyAndclassifier.Key.PathAndQuery.Skip(1).ToArray()), nn);

                // We should receive a valid classifier object back

                var cls = keyAndclassifier.Value.Classify(new
                {
                    x = 12.432,
                    y = Math.Log(12.432)
                });

                foreach (var c in cls)
                {
                    Console.WriteLine(c);
                }

                Assert.That(cls.Any());
            }
        }

        [Test]
        public async Task CreateClassifier_TwoClients()
        {
            var bytes = BitConverter.GetBytes(0);

            Assert.That(bytes.Length, Is.EqualTo(4));

            var endpoint = new Uri("tcp://localhost:9214");

            var data = Functions.NormalRandomDataset(3, 10).Select(x => new
            {
                x = x,
                y = Math.Log(x)
            }).AsQueryable();

            using (var blobs = new InMemoryBlobStore())
            using (var server = new RemoteClassifierTrainingServer(endpoint, blobs))
            using (var client1 = new RemoteClassifierTrainingClient(endpoint))
            using (var client2 = new RemoteClassifierTrainingClient(endpoint))
            {
                server.Start();

                var trainingSet = data.CreatePipeline().AsTrainingSet(x => x.x > 10 ? 'a' : 'b');

                var task1 = client1.CreateClassifier(trainingSet, true);
                var task2 = client2.CreateClassifier(trainingSet, true);
                
                await task1;
                await task2;
            }
        }
    }
}