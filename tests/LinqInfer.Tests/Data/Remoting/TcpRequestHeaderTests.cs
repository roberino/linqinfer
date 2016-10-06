using LinqInfer.Data.Remoting;
using NUnit.Framework;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class TcpRequestHeaderTests : TestFixtureBase
    {
        [Test]
        public void NewInstance_ParsesHttpHeaderCorrectly()
        {
            var data = GetResourceAsBytes("HttpHeaderExample.txt");

            var header = new TcpRequestHeader(data);

            Assert.That(header.HeaderLength, Is.EqualTo(442));
            Assert.That(header.Path, Is.EqualTo("/status"));
            Assert.That(header.TransportProtocol, Is.EqualTo(TransportProtocol.Http));
            Assert.That(header.Headers["Connection"], Is.EqualTo("keep-alive"));
        }
    }
}