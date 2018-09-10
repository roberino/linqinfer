﻿using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class AsyncPipelineExtensionsTests
    {
        [Test]
        public async Task OpenAsMultilayerNetworkClassifier_ExportedClassifierWithExpression()
        {
            var data = Enumerable.Range(0, 10).Select(n => ( x : n, y : n * 7 ));
            
            var pipeline = await data
                .CreatePipeline(x => new BitVector(x.x > 5, x.y > 14), 2)
                .CentreAndScaleAsync();

            var outputs = new[] {"a", "b"};

            var trainingSet = pipeline.AsTrainingSet(x => x.x > 5 ? outputs[0] : outputs[1], outputs);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(2);
            });

            await trainingSet.RunAsync(CancellationToken.None, 550);

            var doc = classifier.ExportData();

            var classifier2 = doc.OpenAsMultilayerNetworkClassifier<(int x, int y), string>();

            var results = classifier2.Classify((3, 33));

            Assert.That(results.Any());
        }

        [Test]
        public async Task AttachMultilayerNetworkClassifier_SoftmaxClassifierWithLinearDataSet_ClassifiesCorrectly()
        {
            var data = new[]
            {
                new
                {
                    x = 10,
                    y = 15,
                    c = "a"
                },
                new
                {
                    x = 11,
                    y = 23,
                    c = "a"
                },
                new
                {
                    x = -11,
                    y = -23,
                    c = "b"
                },
                new
                {
                    x = -15,
                    y = -25,
                    c = "b"
                }
            };

            var pipeline = await data.AsAsyncEnumerator()
                .BuildPipelineAsync(CancellationToken.None);

            pipeline = await pipeline.CentreAndScaleAsync(Range.ZeroToOne);

            Assert.That(pipeline.FeatureExtractor.VectorSize, Is.EqualTo(2));

            var trainingSet = pipeline.AsTrainingSet(x => x.c, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(4, p =>
                {
                    p.HaltingFunction = (_, s) => s.AverageError < 0.4;
                    p.LearningRate = 0.005;
                });
            });

            await trainingSet.RunAsync(CancellationToken.None, 550);

            var results = classifier.Classify(new
            {
                x = 10,
                y = 10,
                c = "?"
            });

            var exportedNetwork = classifier.ExportData();

            Assert.That(exportedNetwork, Is.Not.Null);

            TestFixtureBase.SaveArtifact("nn-350.xml", exportedNetwork.ExportAsXml().Save);

            foreach (var result in results)
            {
                Console.WriteLine(result);
            }

            Assert.That(results.OrderByDescending(x => x.Score).First().ClassType, Is.EqualTo("a"));
        }

        [Test]
        public async Task AttachAndReattachMultilayerNetworkClassifier_ReducesVectorSizeAsSpecified()
        {
            var pipeline = CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(x => x.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
                b.ConfigureSoftmaxNetwork(4)
            );

            await trainingSet.RunAsync(CancellationToken.None);

            var data = classifier.ExportData();

            var classifier2 = trainingSet.AttachMultilayerNetworkClassifier(data);

            Assert.That(classifier2, Is.Not.Null);
        }

        [Test]
        public async Task PrincipalComponentReductionAsync_ReducesVectorSizeAsSpecified()
        {
            var pipeline = CreatePipeline();

            pipeline = await pipeline.PrincipalComponentReductionAsync(2);

            Assert.That(pipeline.FeatureExtractor.VectorSize, Is.EqualTo(2));

            var items = await pipeline.ExtractBatches().ToMemoryAsync(CancellationToken.None, 10);

            Assert.That(items.First().Vector.Size, Is.EqualTo(2));
        }

        [Test]
        public async Task BuildPipeineAsync_ReturnsAsyncPipeline()
        {
            var pipeline = await From.Func(Load)
                .BuildPipelineAsync(
                    CancellationToken.None,
                    new DefaultFeatureExtractionStrategy<TestData.Pirate>(),
                    new CategoricalFeatureExtractionStrategy<TestData.Pirate>());

            var data = await pipeline.ExtractBatches().ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.EqualTo(100));
        }

        [Test]
        public async Task SendAsync_InvokesPublishAsync()
        {
            var pipeline = CreatePipeline();

            var trainingData = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? 'a' : 'b', 'a', 'b');

            var publisher = Substitute.For<IMessagePublisher>();

            await trainingData.SendAsync(publisher, CancellationToken.None);

            await publisher.Received().PublishAsync(Arg.Is<Message>(m => m.Id != null && m.Properties["_Type"] != null && m.Created > DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public async Task AsTrainingSet_UsingOutputParams_ReturnsProcessableTrainingSet()
        {
            var pipeline = CreatePipeline();

            var trainingData = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? 'a' : 'b', 'a', 'b');

            int counter = 0;

            await trainingData
                .ExtractInputOutputIVectorBatches()
                .ProcessUsing(b =>
                {
                    counter++;
                    Assert.That(b.Items.Count == 10);

                    foreach (var item in b.Items)
                    {
                        Assert.That(item.TargetOutput[0], Is.EqualTo(item.Input[0] % 2 == 0 ? 1d : 0d));
                    }

                }, CancellationToken.None);

            Assert.That(counter, Is.GreaterThan(0));
        }

        [Test]
        public async Task CreatePipeline_ReturnsProcessablePipeline()
        {
            var pipeline = CreatePipeline();

            int counter = 0;

            await pipeline.ExtractBatches().ProcessUsing(b =>
            {
                counter++;

                Assert.That(b.Items.Count, Is.EqualTo(10));

                foreach (var item in b.Items)
                {
                    Console.WriteLine(item);
                }

                return true;
            });

            Assert.That(counter, Is.EqualTo(10));
        }

        internal static IAsyncFeatureProcessingPipeline<TestData.Pirate> CreatePipeline()
        {
            var pipeline = new Func<int, AsyncBatch<TestData.Pirate>>(Load).CreatePipeline();

            return pipeline;
        }

        static AsyncBatch<TestData.Pirate> Load(int n)
        {
            var items = Task.FromResult(
                    (IList<TestData.Pirate>)Enumerable.Range(0, 10)
                    .Select(x => new TestData.Pirate()
                    {
                        Age = x,
                        Gold = n,
                        Ships = x * n,
                        IsCaptain = ((x * n) % 3) == 0,
                        Category = ((x * n) % 3) == 0 ? "a" : "b"
                    })
                    .ToList()
                    );

            if (n > 9) throw new InvalidOperationException();

            return new AsyncBatch<TestData.Pirate>(items, n == 9, n);
        }
    }
}