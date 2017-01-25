using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System.Linq;
using System.Text;

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

            var headerExContent = Encoding.ASCII.GetString(data.Take(header.HeaderLength).ToArray());

            Assert.That(header.Path, Is.EqualTo("/status"));
            Assert.That(header.TransportProtocol, Is.EqualTo(TransportProtocol.Http));
            Assert.That(header.Headers["Connection"][0], Is.EqualTo("keep-alive"));

            // Assert.That(header.HeaderLength, Is.EqualTo(453));
        }
    }
}