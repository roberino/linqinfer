using LinqInfer.Data.Remoting;
using LinqInfer.Text.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public class HttpDocumentServices : IDisposable
    {
        public HttpDocumentServices(ITokeniser tokeniser = null,
            TextMimeType mimeType = TextMimeType.Default,
            Func<Uri, bool> linkFilter = null,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null) : this(new HttpBasicClient(), tokeniser, mimeType, linkFilter, nodeFilter, linkExtractor)
        {
        }

        public HttpDocumentServices(
            IHttpClient httpClient,
            ITokeniser tokeniser = null,
            TextMimeType mimeType = TextMimeType.Default,
            Func<Uri, bool> linkFilter = null,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null)
        {
            DocumentClient = new HttpDocumentClient(
                httpClient,
                tokeniser,
                mimeType,
                nodeFilter,
                linkExtractor
                );

            DocumentCrawler = new HttpDocumentCrawler(DocumentClient, linkFilter);
        }

        public HttpDocumentServices(Func<Uri, bool> linkFilter) : this(null, TextMimeType.Default, linkFilter)
        {
        }

        public HttpDocumentClient DocumentClient { get; }

        internal HttpDocumentCrawler DocumentCrawler { get; }

        public ICorpus CreateVirtualCorpus(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            return new VirtualCorpus(
                DocumentCrawler.CrawlDocuments(rootUri, documentFilter, maxDocs, targetElement)
                .Select(async t =>
                {
                    var docs = await t;

                    return (IList<IToken>)docs.SelectMany(d => d.Tokens).ToList();
                }));
        }

        public async Task<ICorpus> CreateCorpus(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var corpus = new Corpus();

            await DocumentCrawler.CrawlDocuments(rootUri, d =>
            {
                foreach (var token in d.Tokens)
                {
                    corpus.Append(token);
                }
            }, documentFilter, maxDocs, targetElement);

            return corpus;
        }

        public async Task<IDocumentIndex> CreateIndex(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var index = new DocumentIndex(DocumentClient.Tokeniser);

            await DocumentCrawler.CrawlDocuments(rootUri, d =>
            {
                index.IndexDocument(d);
            }, documentFilter, maxDocs, targetElement);

            return index;
        }

        public IEnumerable<Uri> VisitedUrls => DocumentClient.VisitedUrls;

        public void Dispose()
        {
            DocumentClient.Dispose();
        }
    }
}