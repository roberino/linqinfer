using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class InMemorySinkTests
    {
        [Test]
        public async Task WhenMaxCapacitySet_LimitsDataReceived()
        {
            var sink = new InMemorySink<byte[]>(3);

            var source = From.Func(LoadData, 123);

            var pipe = source.CreatePipe().RegisterSinks(sink);

            await pipe.RunAsync(new CancellationToken());

            Assert.That(sink.Count, Is.EqualTo(3));
            Assert.That(sink.CanReceive, Is.False);
        }

        [Test]
        public async Task WhenMaxCapacityNotSet_DoesNotLimitDataReceived()
        {
            var sink = new InMemorySink<byte[]>();

            var source = From.Func(LoadData, 123);

            var pipe = source.CreatePipe().RegisterSinks(sink);

            await pipe.RunAsync(new CancellationToken());

            Assert.That(sink.Count, Is.EqualTo(40));
            Assert.That(sink.CanReceive, Is.True);
        }

        private AsyncBatch<byte[]> LoadData(int n)
        {
            var data = (IList<byte[]>)Enumerable.Range(0, 10).Select(_ => new byte[] { 1, 3, 5, 8 }).ToList();
            return new AsyncBatch<byte[]>(Task.FromResult(data), n == 3, n);
        }
    }
}