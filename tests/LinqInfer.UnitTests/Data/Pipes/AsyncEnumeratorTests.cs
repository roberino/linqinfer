using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class AsyncEnumeratorTests
    {
        [Test]
        public void EstimatedTotalCount_ReturnsCorrectValue()
        {
            var asyncEnum = From.Func(Load, 100);

            Assert.That(asyncEnum.EstimatedTotalCount, Is.EqualTo(100));
        }

        [Test]
        public async Task WhenLimitSet_RestrictsTheNumberOfItemsReturned()
        {
            var asyncEnum = From
                .Func(Load)
                .Limit(53);

            var data = await asyncEnum.ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.EqualTo(53));
        }

        [Test]
        public async Task WhenNoLimitSet_FetchesAllItems()
        {
            var asyncEnum = From.Func(Load);

            var data = await asyncEnum.ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.EqualTo(100));
        }

        [Test]
        public async Task WhenFiltered_RestrictsItemsToMatchingPredicate()
        {
            var asyncEnum = From
                .Func(Load)
                .Filter(s => s.StartsWith("5_"));

            var data = await asyncEnum.ToMemoryAsync(CancellationToken.None);

            Assert.That(data.All(s => s.StartsWith("5_")));
        }

        private static AsyncBatch<string> Load(int n)
        {
            var items = Task.FromResult(
                    (IList<string>)Enumerable.Range(0, 10)
                    .Select(x => $"{n}_{Guid.NewGuid()}")
                    .ToList()
                    );

            if (n > 9) throw new InvalidOperationException();

            return new AsyncBatch<string>(items, n == 9, n);
        }
    }
}