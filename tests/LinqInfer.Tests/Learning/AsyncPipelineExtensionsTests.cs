using LinqInfer.Data;
using LinqInfer.Learning;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using static LinqInfer.Tests.TestData;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class AsyncPipelineExtensionsTests
    {
        [Test]
        public async Task CreatePipeline_ReturnsProcessablePipeline()
        {
            var pipeline = new Func<int, AsyncBatch<Pirate>>(Load).CreatePipeline();

            int counter = 0;

            await pipeline.ExtractBatches().ProcessUsing(b =>
            {
                counter++;

                Assert.That(b.Items.Count, Is.EqualTo(10));

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