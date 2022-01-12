using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class StatisticSinkTests
    {
        [Test]
        public async Task WhenBatchesReceived_ThenCountersIncremented()
        {
            var sink = new StatisticSink<string>();

            Assert.That(sink.CanReceive, Is.True);
            
            await WhenBatchesReceived(sink);

            Assert.That(sink.BatchesReceived, Is.EqualTo(2));
            Assert.That(sink.ItemsReceived, Is.EqualTo(5));
            Assert.That(sink.Elapsed.TotalMilliseconds, Is.GreaterThan(0));
            Assert.That(sink.AverageBatchesPerSecond, Is.GreaterThan(0));
        }

        [Test]
        public async Task WhenBatchesReceived_ThenAverageBatchesPerSecondCalculatedCorrectly()
        {
            var timer = Substitute.For<IStopwatch>();
            var sink = new StatisticSink<string>(timer);

            await WhenBatchesReceived(sink);

            timer.Elapsed.Returns(TimeSpan.FromMilliseconds(1957));

            Assert.That(sink.AverageBatchesPerSecond, Is.EqualTo(1.0219724067450178d));
            Assert.That(sink.AverageItemsPerSecond, Is.EqualTo(2.5549310168625445d));
        }

        async Task WhenBatchesReceived(StatisticSink<string> sink)
        {
            var batch1 = new AsyncBatch<string>(LoadData("a", "b"), false, 1);
            var batch2 = new AsyncBatch<string>(LoadData("c", "d", "e"), true, 2);

            foreach (var batch in new[] { batch1, batch2 })
            {
                await batch.ItemsLoader;

                await sink.ReceiveAsync(batch, CancellationToken.None);
            }
        }

        async Task<IList<string>> LoadData(params string[] items)
        {
            await Task.Delay(5);

            return items;
        }
    }
}