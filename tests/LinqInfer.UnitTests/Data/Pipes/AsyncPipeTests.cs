using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using NSubstitute;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Data.Pipes
{
    [TestFixture]
    public class AsyncPipeTests
    {
        [Test]
        public async Task RunAsync_WhenGivenListOfItems_SendsThemToSink()
        {
            var source = new[] { 'a', 'b', 'c', 'd' }.AsAsyncEnumerator();

            var pipe = new AsyncPipe<char>(source);

            var sink = Substitute.For<IAsyncSink<char>>();

            sink.CanReceive.Returns(true);

            var cancelToken = new CancellationToken();

            pipe.RegisterSinks(sink);

            await pipe.RunAsync(cancelToken);

            await sink.Received().ReceiveAsync(Arg.Is<IBatch<char>>(b => b.Items.Count == 4 && b.Items.AllEqual(n => (char)('a' + n))), cancelToken);
        }

        [Test]
        public async Task RunAsync_WhenSinkCanReceiveFalse_DoesNotSendDataToSink()
        {
            var source = new[] { 'a', 'b', 'c', 'd' }.AsAsyncEnumerator();

            var pipe = new AsyncPipe<char>(source);

            var sink = Substitute.For<IAsyncSink<char>>();

            sink.CanReceive.Returns(false);

            var cancelToken = new CancellationToken();

            pipe.RegisterSinks(sink);

            await pipe.RunAsync(cancelToken);

            await sink.DidNotReceive().ReceiveAsync(Arg.Is<IBatch<char>>(b => b.Items.Count == 4 && b.Items.AllEqual(n => (char)('a' + n))), cancelToken);
        }

        [Test]
        public void Disposing_WhenDisposedCalled_FiresEvent()
        {
            var source = From.EmptySource<int>();

            var pipe = new AsyncPipe<int>(source);

            bool disposingFired = false;

            pipe.Disposing += (s, e) =>
            {
                disposingFired = true;
            };

            pipe.Dispose();

            Assert.That(disposingFired);
        }
    }
}