using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    internal class HttpTokenReader : IDisposable
    {
        private const int MaxVisited = 350;

        private readonly HttpClient _client;
        private readonly ITokeniser _tokeniser;
        private readonly Encoding _encoding;
        private readonly TextMimeType _mimeType;
        private readonly Func<XNode, bool> _nodeFilter;
        private readonly Func<XElement, IEnumerable<string>> _linkExtractor;
        private readonly Func<Uri, bool> _linkFilter;
        private readonly HashSet<Uri> _visited;

        private int _maxDepth = 10;

        public HttpTokenReader(ITokeniser tokeniser = null, Encoding encoding = null, TextMimeType mimeType = TextMimeType.Default, Func<Uri, bool> linkFilter = null, Func<XNode, bool> nodeFilter = null, Func<XElement, IEnumerable<string>> linkExtractor = null)
        {
            _client = new HttpClient();
            _visited = new HashSet<Uri>();
            _tokeniser = tokeniser ?? new Tokeniser();
            _encoding = encoding ?? Encoding.UTF8;
            _mimeType = mimeType;
            _nodeFilter = nodeFilter ?? HtmlTextNodeFilter.Filter;
            _linkFilter = linkFilter ?? (_ => true);
            _linkExtractor = linkExtractor ?? (e => e.Descendants().Where(x => x.Name.LocalName == "a").Select(a => a.Attribute("href")).Where(a => a != null).Select(a => a.Value));
        }

        public HttpTokenReader(Func<Uri, bool> linkFilter) : this()
        {
            _linkFilter = linkFilter;
        }

        public bool FollowLinks
        {
            get
            {
                return _maxDepth > 0;
            }
            set
            {
                _maxDepth = value ? 10 : 0;
            }
        }

        public Task Read(Uri rootUri, Func<Tuple<Uri, IEnumerable<IToken>>, bool> tokenProcessor, Func<XElement, XElement> targetElement = null)
        {
            return Read(rootUri, tokenProcessor, targetElement, 0);
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

        protected async Task Read(Uri uri, Func<Tuple<Uri, IEnumerable<IToken>>, bool> tokenProcessor, Func<XElement, XElement> targetElement, int depth)
        {
            if (depth > _maxDepth || _visited.Count > MaxVisited) return;

            var mimeType = _mimeType;

            var response = await _client.GetAsync(uri);

            if (mimeType == TextMimeType.Default)
            {
                IEnumerable<string> mimes;

                if (response.Headers.TryGetValues("Content-Type", out mimes))
                {
                    mimeType = Parse(mimes.First());
                }
            }

            if (mimeType == TextMimeType.Default) mimeType = TextMimeType.Html;

            var stream = await response.Content.ReadAsStreamAsync();

            using (stream)
            {
                if (mimeType == TextMimeType.Xml)
                {
                    var doc = XDocument.Load(stream);

                    XElement root = targetElement == null ? doc.Root : targetElement(doc.Root);

                    if (root != null && tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, ExtractTokens(root))))
                    {
                        await FollowLinksAndProcess(uri, _linkExtractor(doc.Root), tokenProcessor, targetElement, depth);
                    }
                }
                else
                {
                    using (var reader = new StreamReader(stream, _encoding))
                    {
                        if (mimeType == TextMimeType.Html)
                        {
                            var parser = new HtmlParser(_encoding);

                            var elements = parser.Parse(reader);
                            var selected = targetElement != null ? elements.Where(e => e.NodeType == System.Xml.XmlNodeType.Element).Select(e => targetElement((XElement)e)) : elements;

                            if (tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, selected.SelectMany(e => ExtractTokens(e)))))
                            {
                                foreach (var e in selected.Where(x => x != null && x.NodeType == System.Xml.XmlNodeType.Element).Cast<XElement>())
                                    await FollowLinksAndProcess(uri, _linkExtractor(e), tokenProcessor, targetElement, depth);
                            }
                        }
                        else
                        {
                            if (mimeType == TextMimeType.Plain)
                            {
                                var docContent = reader.ReadToEnd();

                                if (tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, _tokeniser.Tokenise(docContent))))
                                {
                                    await FollowLinksAndProcess(uri, _linkExtractor(new XElement("x", docContent)), tokenProcessor, targetElement, depth);
                                }
                            }
                            else
                            {
                                throw new NotSupportedException(mimeType.ToString());
                            }
                        }
                    }
                }
            }
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

        private byte GetTokenWeight(XText text)
        {
            // Get the heading weight
            
            var h = new Regex("^h([1-6])$");
            var x = new Regex("^p|div|body$");

            var cur = text.Parent;

            while (cur != null)
            {
                var matches = h.Matches(cur.Name.LocalName);

                if (matches.Count > 0)
                {
                    return byte.Parse(matches[0].Groups[1].Value);
                }

                if (x.IsMatch(cur.Name.LocalName)) break;
            }

            return 0;
        }

        private async Task FollowLinksAndProcess(Uri baseUri, IEnumerable<string> links, Func<Tuple<Uri, IEnumerable<IToken>>, bool> tokenProcessor, Func<XElement, XElement> targetElement, int depth = 0)
        {
            if (depth > _maxDepth || _visited.Count > MaxVisited) return;

            foreach (var link in links.Select(l => GetUri(baseUri, l)))
            {
                if (!_visited.Contains(link) && _linkFilter(link))
                {
                    _visited.Add(link);

                    await Read(link, tokenProcessor, targetElement, depth + 1);
                }
            }
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
