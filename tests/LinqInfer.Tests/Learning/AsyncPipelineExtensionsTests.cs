using LinqInfer.Data;
using LinqInfer.Learning;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using static LinqInfer.Tests.TestData;
using System.Threading;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Data.Remoting;
using NSubstitute;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class AsyncPipelineExtensionsTests
    {
        [Test]
        public async Task SendAsync_InvokesPublishAsync()
        {
            var pipeline = new Func<int, AsyncBatch<Pirate>>(Load).CreatePipeline();

            var trainingData = pipeline.AsTrainingSet(p => p.Age % 2 == 0 ? 'a' : 'b', 'a', 'b');

            var publisher = Substitute.For<IMessagePublisher>();

            await trainingData.SendAsync(publisher, CancellationToken.None);
            
            await publisher.Received().PublishAsync(Arg.Is<Message>(m => m.Id != null && m.Properties["_Type"] != null && m.Created > DateTime.UtcNow.AddMinutes(-1)));
        }

        [Test]
        public async Task AsTrainingSet_UsingOutputParams_ReturnsProcessableTrainingSet()
        {
            var pipeline = new Func<int, AsyncBatch<Pirate>>(Load).CreatePipeline();

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
        }

        [Test]
        public async Task CreatePipeline_ReturnsProcessablePipeline()
        {
            var pipeline = new Func<int, AsyncBatch<Pirate>>(Load).CreatePipeline();

            int counter = 0;

            await pipeline.ExtractBatches().ProcessUsing(b =>
            {
                counter++;

                Assert.That(b.Items.Count, Is.EqualTo(10));
                
                foreach(var item in b.Items)
                {
                    Console.WriteLine(item);
                }

                return true;
            });

            Assert.That(counter, Is.EqualTo(10));
        }

        private static AsyncBatch<Pirate> Load(int n)
        {
            var items = Task.FromResult(
                    (IList<Pirate>)Enumerable.Range(0, 10)
                    .Select(x => new Pirate() { Age = x, Gold = n, Ships = x * n, IsCaptain = ((x * n) % 3) == 0 })
                    .ToList()
                    );

            if (n > 9) throw new InvalidOperationException();

            return new AsyncBatch<Pirate>(items, n == 9, n);
        }
    }
}