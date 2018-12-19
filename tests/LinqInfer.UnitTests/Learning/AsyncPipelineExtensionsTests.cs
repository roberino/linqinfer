using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class AsyncPipelineExtensionsTests
    {
        [Test]
        public async Task PrincipalComponentReductionAsync_ReducesVectorSizeAsSpecified()
        {
            var pipeline = TestSamples.CreatePipeline();

            pipeline = await pipeline.PrincipalComponentReductionAsync(2);

            Assert.That(pipeline.FeatureExtractor.VectorSize, Is.EqualTo(2));

            var items = await pipeline.ExtractBatches().ToMemoryAsync(CancellationToken.None, 10);

            Assert.That(items.First().Vector.Size, Is.EqualTo(2));
        }

        [Test]
        public async Task BuildPipeineAsync_ReturnsAsyncPipeline()
        {
            var pipeline = await From.Func(TestSamples.Load)
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
            var pipeline = TestSamples.CreatePipeline();

            var trainingData = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? 'a' : 'b', 'a', 'b');

            var publisher = Substitute.For<IMessagePublisher>();

            await trainingData.SendAsync(publisher, CancellationToken.None);

            await publisher.Received().PublishAsync(Arg.Is<Message>(m => m.Id != null && m.Properties["_Type"] != null && m.Created > DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public async Task AsTrainingSet_UsingOutputParams_ReturnsProcessableTrainingSet()
        {
            var pipeline = TestSamples.CreatePipeline();

            var trainingData = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? 'a' : 'b', 'a', 'b');

            int counter = 0;

            await trainingData
                .Source
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
            var pipeline = TestSamples.CreatePipeline();

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
    }
}