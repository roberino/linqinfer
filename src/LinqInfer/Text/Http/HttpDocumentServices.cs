using LinqInfer.Data.Remoting;
using LinqInfer.Text.Analysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public class HttpDocumentServices : IDisposable
    {
        private const int MaxVisited = 350;

        private readonly IHttpClient _client;
        private readonly ITokeniser _tokeniser;
        private readonly TextMimeType _mimeType;
        private readonly Func<XNode, bool> _nodeFilter;
        private readonly Func<XElement, IEnumerable<string>> _linkExtractor;
        private readonly Func<Uri, bool> _linkFilter;
        private readonly HashSet<Uri> _visited;

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
            _client = httpClient;
            _visited = new HashSet<Uri>();
            _tokeniser = tokeniser ?? new Tokeniser();
            _mimeType = mimeType;
            _nodeFilter = nodeFilter ?? HtmlTextNodeFilter.Filter;
            _linkFilter = linkFilter ?? (_ => true);
            _linkExtractor = linkExtractor ?? (e => e.Descendants().Where(x => x.Name.LocalName == "a").Select(a => a.Attribute("href")).Where(a => a != null).Select(a => a.Value));
        }

        public HttpDocumentServices(Func<Uri, bool> linkFilter) : this()
        {
            _linkFilter = linkFilter;
        }

        public async Task<ICorpus> CreateCorpus(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var corpus = new Corpus();

            await CrawlDocuments(rootUri, d =>
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
            var index = new DocumentIndex(_tokeniser);

            await CrawlDocuments(rootUri, d =>
            {
                index.IndexDocument(d);
            }, documentFilter, maxDocs, targetElement);

            return index;
        }

        public IEnumerable<Task<IList<HttpDocument>>> CrawlDocuments(Uri rootUri, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var pending = new Queue<IList<Uri>>();

            pending.Enqueue(new[] { rootUri });
            int count = 1;

            var f = new Func<Task<IList<HttpDocument>>>(async () =>
            {
                var docs = new List<HttpDocument>();

                foreach (var child in await FollowLinks(pending.Dequeue().Take(maxDocs - count), targetElement))
                {
                    if ((documentFilter?.Invoke(child)).GetValueOrDefault(true))
                    {
                        docs.Add(child);

                        count++;

                        pending.Enqueue(child.Links.Select(l => l.Url).Where(_linkFilter).ToList());
                    }

                    if (count >= maxDocs) break;
                }

                return docs;
            });

            while (count < maxDocs && pending.Any())
            {
                yield return f();
            }
        }

        public async Task CrawlDocuments(Uri rootUri, Action<HttpDocument> documentAction, Func<HttpDocument, bool> documentFilter = null, int maxDocs = 50, Func<XElement, XElement> targetElement = null)
        {
            var pending = new Queue<IList<Uri>>();

            pending.Enqueue(new[] { rootUri });
            int count = 1;

            while (count < maxDocs && pending.Any())
            {
                foreach (var child in await FollowLinks(pending.Dequeue().Take(maxDocs - count), targetElement))
                {
                    if ((documentFilter?.Invoke(child)).GetValueOrDefault(true))
                    {
                        documentAction(child);

                        count++;

                        pending.Enqueue(child.Links.Select(l => l.Url).Where(_linkFilter).ToList());
                    }

                    if (count >= maxDocs) break;
                }
            }
        }

        public Task<HttpDocument> GetDocument(Uri rootUri, Func<XElement, XElement> targetElement = null)
        {
            var current = Read(rootUri, targetElement);

            _visited.Add(rootUri);

            return current;
        }

        public IEnumerable<Uri> VisitedUrls
        {
            get
            {
                return _visited;
            }
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
                mimeType = Parse(response.Header.ContentMimeType);
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
            foreach (var node in AllText(doc))
            {
                if (_nodeFilter(node))
                {
                    foreach (var token in _tokeniser.Tokenise(node.Value))
                    {
                        if(token is Token)
                        {
                            ((Token)token).Weight = GetTokenWeight(node);
                        }

                        yield return token;
                    }
                }
            }
        }

        private async Task<IList<HttpDocument>> FollowLinks(IEnumerable<Uri> links, Func<XElement, XElement> targetElement = null)
        {
            var docs = links.Where(l => !_visited.Contains(l)).Select(l => GetDocument(l, targetElement)).ToList();

            await Task.WhenAll(docs);

            return docs.Select(t => t.Result).Where(d => d != null).ToList();
        }

        private byte GetTokenWeight(XText text)
        {
            // Get the heading weight
            
            var h = new Regex("^h([1-6])$");
            var x = new Regex("^p|div|body$");

            var cur = text.Parent;

            while (cur != null && x.IsMatch(cur.Name.LocalName))
            {
                var matches = h.Matches(cur.Name.LocalName);

                if (matches.Count > 0)
                {
                    return byte.Parse(matches[0].Groups[1].Value);
                }

                cur = cur.Parent;
            }

            return 0;
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

        private IEnumerable<XText> AllText(XNode currentNode)
        {
            if (currentNode == null) yield break;

            if (currentNode.NodeType == System.Xml.XmlNodeType.Text)
            {
                yield return ((XText)currentNode);
                yield break;
            }

            if (currentNode.NodeType != System.Xml.XmlNodeType.Element) yield break;

            var parent = ((XElement)currentNode);

            foreach (var node in parent.Nodes())
            {
                if (node.NodeType == System.Xml.XmlNodeType.Text)
                {
                    yield return (XText)node;
                }
                else
                {
                    if (node.NodeType == System.Xml.XmlNodeType.Element)
                    {
                        foreach (var text in AllText((XElement)node))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }

        private TextMimeType Parse(string mimeType)
        {
            switch (mimeType)
            {
                case "text/html":
                    return TextMimeType.Html;
                case "text/plain":
                    return TextMimeType.Plain;
                case "text/xml":
                    return TextMimeType.Xml;
                case "text/json":
                case "application/json":
                    return TextMimeType.Json;
                default:
                    return TextMimeType.Default;
            }
        }

        private static class HtmlTextNodeFilter
        {
            private static HashSet<string> _semanticElements;

            static HtmlTextNodeFilter()
            {
                _semanticElements = new HashSet<string>(new[] { "a", "span", "li", "p", "b", "strong", "i", "em" });
            }

            public static bool Filter(XNode node)
            {
                var cur = node;

                while (cur != null)
                {
                    if (cur.NodeType == System.Xml.XmlNodeType.Element && _semanticElements.Contains(((XElement)cur).Name.LocalName))
                    {
                        return true;
                    }

                    cur = cur.Parent;
                }

                return false;
            }
        }
    }
}