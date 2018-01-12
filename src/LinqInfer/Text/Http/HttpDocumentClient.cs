using LinqInfer.Data.Remoting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public class HttpDocumentClient : IDisposable
    {
        private const int MaxVisited = 350;

        private readonly IHttpClient _client;
        private readonly ITokeniser _tokeniser;
        private readonly TextMimeType _mimeType;
        private readonly Func<XNode, bool> _nodeFilter;
        private readonly Func<XElement, IEnumerable<string>> _linkExtractor;
        private readonly HashSet<Uri> _visited;

        internal HttpDocumentClient(
            IHttpClient httpClient,
            ITokeniser tokeniser = null,
            TextMimeType mimeType = TextMimeType.Default,
            Func<XNode, bool> nodeFilter = null, 
            Func<XElement, IEnumerable<string>> linkExtractor = null)
        {
            _client = httpClient;
            _visited = new HashSet<Uri>();
            _tokeniser = tokeniser ?? new Tokeniser();
            _mimeType = mimeType;
            _nodeFilter = nodeFilter ?? HtmlTextNodeFilter.Filter;
            _linkExtractor = linkExtractor ?? (e => e.Descendants().Where(x => x.Name.LocalName == "a").Select(a => a.Attribute("href")).Where(a => a != null).Select(a => a.Value));
        }

        public ITokeniser Tokeniser => _tokeniser;

        public IEnumerable<Uri> VisitedUrls => _visited;

        public Task<HttpDocument> GetDocument(Uri rootUri, Func<XElement, XElement> targetElement = null)
        {
            var current = Read(rootUri, targetElement);

            _visited.Add(rootUri);

            return current;
        }

        public async Task<IList<HttpDocument>> FollowLinks(IEnumerable<Uri> links, Func<XElement, XElement> targetElement = null)
        {
            var docs = links.Where(l => !_visited.Contains(l)).Select(l => GetDocument(l, targetElement)).ToList();

            await Task.WhenAll(docs);

            return docs.Select(t => t.Result).Where(d => d != null).ToList();
        }

        public void Dispose()
        {
            _client.Dispose();
        }

        protected async Task<HttpDocument> Read(Uri uri, Func<XElement, XElement> targetElement)
        {
            var mimeType = _mimeType;

            var response = await _client.GetAsync(uri);

            var enc = response.Header.TextEncoding ?? Encoding.UTF8;

            if (mimeType == TextMimeType.Default)
            {
                mimeType = TextMimeTypeParser.Parse(response.Header.ContentMimeType);
            }

            if (mimeType == TextMimeType.Default) mimeType = TextMimeType.Html;

            using (response.Content)
            {
                using (var reader = new StreamReader(response.Content, enc))
                {
                    if (mimeType == TextMimeType.Xml)
                    {
                        var doc = XDocument.Load(reader);

                        XElement root = targetElement == null ? doc.Root : targetElement(doc.Root);

                        if (root != null)
                        {
                            var links = _linkExtractor(doc.Root);

                            var hdoc = new HttpDocument(uri, ExtractTokens(root), links.Select(l => CreateRelLink(uri, l)).Distinct().ToList(), response.Header.Headers);

                            return hdoc;
                        }
                    }
                    else
                    {
                        if (mimeType == TextMimeType.Html)
                        {
                            var parser = new HtmlParser();

                            var elements = parser.Parse(reader);

                            var head = (elements.FirstOrDefault(e => e.NodeType == System.Xml.XmlNodeType.Element) as XElement)?.Elements(XName.Get("head"));
                            var title = head?.Elements("title").FirstOrDefault();
                            
                            var selected = targetElement != null ? elements.Where(e => e.NodeType == System.Xml.XmlNodeType.Element).Select(e => targetElement((XElement)e)) : elements;

                            {
                                var links = selected.Where(x => x != null && x.NodeType == System.Xml.XmlNodeType.Element).Cast<XElement>().SelectMany(x => _linkExtractor(x));

                                var hdoc = new HttpDocument(
                                    uri,
                                    selected.SelectMany(e => ExtractTokens(e)),
                                    links
                                        .Select(l => CreateRelLink(uri, l))
                                        .Distinct()
                                        .ToList(),
                                    response.Header.Headers
                                        );

                                if (head != null)
                                {
                                    if (title != null)
                                    {
                                        hdoc.Title = title.Value;
                                    }
                                }

                                return hdoc;
                            }
                        }
                        else
                        {
                            if (mimeType == TextMimeType.Plain)
                            {
                                var docContent = reader.ReadToEnd();

                                {
                                    var hdoc = new HttpDocument(uri, _tokeniser.Tokenise(docContent), Enumerable.Empty<RelativeLink>(), response.Header.Headers);

                                    return hdoc;
                                }
                            }
                            else
                            {
                                throw new NotSupportedException(mimeType.ToString());
                            }
                        }
                    }
                }

                return null;
            }
        }

        protected virtual RelativeLink CreateRelLink(Uri baseUri, string relLink, string relationship = "link")
        {
            if (relLink == null) return new RelativeLink() { Url = baseUri, Rel = "self" };

            var hash = relLink.IndexOf('#');

            return new RelativeLink() { Url = new Uri(baseUri, (hash > -1) ? relLink.Substring(0, hash) : relLink), Rel = relationship };
        }

        protected virtual IEnumerable<IToken> ExtractTokens(XNode doc)
        {
            return XmlTextExtractor.ExtractTokens(doc, _nodeFilter, _tokeniser);
        }

        private Uri GetUri(Uri baseUri, string relativeUri)
        {
            try
            {
                return new Uri(baseUri, relativeUri);
            }
            catch
            {
                return null;
            }
        }
    }
}