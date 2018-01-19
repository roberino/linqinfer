using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using LinqInfer.Text.Http;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text.Http
{
    [TestFixture]
    public class HttpDocumentServicesTests
    {
        private readonly Uri _rootUrl = new Uri("http://test.x");

        [Test]
        public async Task CreateDocumentSource_WhenProcessed_ReturnsExpectedBatchesOfDocuments()
        {
            var services = CreateSut();

            var source = services.CreateDocumentSource(_rootUrl, new HttpDocumentCrawlerOptions() { BatchSize = 3, MaxNumberOfDocuments = 5 });

            var items = await source.ToMemoryAsync(CancellationToken.None);

            Assert.That(items.Count, Is.EqualTo(5));
        }

        private HttpDocumentServices CreateSut()
        {
            var httpClient = Substitute.For<IHttpClient>();
            var contentReader = Substitute.For<IContentReader>();
            var content = new MemoryStream();
            var doc = HttpDocument.CreateEmpty(_rootUrl);
            var response = new TcpResponse(TransportProtocol.Http, content);

            httpClient.GetAsync(_rootUrl).Returns(response);
            contentReader.ReadAsync(_rootUrl, content, Arg.Any<IDictionary<string, string[]>>(), Arg.Any<string>(), Encoding.UTF8, Arg.Any<Func<XElement, XElement>>()).Returns(doc);

            return new HttpDocumentServices(httpClient, contentReader);
        }
    }
}