using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using NSubstitute;
using NUnit.Framework;
using System.Linq;
using LinqInfer.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Pipes
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

            var cancelToken = new CancellationToken();

            pipe.RegisterSinks(sink);

            await pipe.RunAsync(cancelToken);

            await sink.Received().ReceiveAsync(Arg.Is<IBatch<char>>(b => b.Items.Count == 4 && b.Items.AllEqual(n => (char)('a' + n))), cancelToken);
        }
    }
}