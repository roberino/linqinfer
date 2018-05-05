using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LinqInfer.Data.Pipes;
using LinqInfer.Data.Remoting;
using LinqInfer.Text;
using LinqInfer.Text.Http;
using NSubstitute;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.Http
{
    [TestFixture]
    public class HttpDocumentServicesTests
    {
        private readonly Uri _rootUrl = new Uri("http://test.x");

        private HttpDocumentServices _sut;
        private IHttpClient _httpClient;
        private IContentReader _contentReader;
        private IList<HttpDocument> _results;

        [Test]
        public async Task CreateDocumentSource_WhenNoLinksToFollow_Returns1Document()
        {
            var services = CreateSut();

            WhenTheResponseIsReturned(_rootUrl);

            await WhenIGetDocumentsFromSource();

            Assert.That(_results.Count, Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(3)]
        public async Task CreateDocumentSource_WhenDocumentWithLinks_ReturnsDocuments(int numberOfLinks)
        {
            var linksToFollow = Enumerable.Range(0, numberOfLinks)
                .Select(n => new RelativeLink()
                {
                    Url = new Uri("http://xxx/" + n)
                })
                .ToList();

            var docWithLinks = new HttpDocument(_rootUrl, Enumerable.Empty<IToken>(), linksToFollow);

            CreateSut();

            WhenTheResponseIsReturned(_rootUrl, docWithLinks);

            foreach (var item in linksToFollow)
            {
                WhenTheResponseIsReturned(item.Url);
            }

            await WhenIGetDocumentsFromSource();

            Assert.That(_results.Count, Is.EqualTo(numberOfLinks + 1));
        }

        private async Task WhenIGetDocumentsFromSource(HttpDocumentCrawlerOptions crawlerOptions = null)
        {
            var opts = crawlerOptions ?? new HttpDocumentCrawlerOptions() { BatchSize = 3, MaxNumberOfDocuments = 5 };

            var source = _sut.CreateDocumentSource(_rootUrl, opts);

            _results = (await source.ToMemoryAsync(CancellationToken.None)).ToList();
        }

        private HttpDocumentServices CreateSut()
        {
            _httpClient = Substitute.For<IHttpClient>();
            _contentReader = Substitute.For<IContentReader>();

            _sut = new HttpDocumentServices(_httpClient, _contentReader);

            return _sut;
        }

        private void WhenTheResponseIsReturned(Uri uri, HttpDocument httpDocument = null)
        {
            var content = new MemoryStream();
            var doc = httpDocument ?? HttpDocument.CreateEmpty(_rootUrl);
            var response = new TcpResponse(TransportProtocol.Http, content);

            _httpClient.GetAsync(uri).Returns(response);
            _contentReader.ReadAsync(uri, content, Arg.Any<IDictionary<string, string[]>>(), Arg.Any<string>(), Encoding.UTF8, Arg.Any<Func<XElement, XElement>>()).Returns(doc);
        }
    }
}