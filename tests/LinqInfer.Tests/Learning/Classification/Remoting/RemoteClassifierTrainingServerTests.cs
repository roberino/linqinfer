﻿using LinqInfer.Data;
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
        public async Task CreateClassifier_SavesOutput()
        {
            var bytes = BitConverter.GetBytes(0);

            Assert.That(bytes.Length, Is.EqualTo(4));

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

                var keyAndclassifier = await client.CreateClassifier(pipeline, x => x.x > 10 ? 'a' : 'b', true);
                
                // We should be able to restore the raw blob back into a network

                var nn = new MultilayerNetwork(new NetworkParameters(1, 1));

                blobs.Restore(keyAndclassifier.Key, nn);

                // We should receive a valid classifier object back

                var cls = keyAndclassifier.Value.Classify(new
                {
                    x = 12.432,
                    y = Math.Log(12.432)
                });

                foreach(var c in cls)
                {
                    Console.WriteLine(c);
                }

                Assert.That(cls.Any());
            }
        }
    }
}