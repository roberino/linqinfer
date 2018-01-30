using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System.Threading;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class HttpServerTests
    {
        [Test]
        public void Start_Then_Stop_ReturnsExpectedStatus()
        {
            using (var server = new HttpServer("test-server-1", 8093))
            {
                server.Start();

                Assert.That(server.Status, Is.EqualTo(ServerStatus.Running));

                server.Stop();

                Assert.That(server.Status == ServerStatus.ShuttingDown || server.Status == ServerStatus.Stopped);
            }
        }

        [Test]
        public void Start_Then_Stop_ThenStart_ReturnsExpectedStatus()
        {
            using (var server = new HttpServer("test-server-1", 8093))
            {
                server.Start();
                server.Stop(true);
                server.Start();

                Assert.That(server.Status, Is.EqualTo(ServerStatus.Running));
            }
        }
    }
}
