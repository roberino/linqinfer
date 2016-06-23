using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    internal class HttpSemanticReader
    {
        private readonly HttpClient _client;
        private readonly ITokeniser _tokeniser;
        private readonly Encoding _encoding;
        private readonly TextMimeType _mimeType;
        private readonly Func<XNode, bool> _nodeFilter;

        public HttpSemanticReader(ITokeniser tokeniser = null, Encoding encoding = null, TextMimeType mimeType = TextMimeType.Default, Func<XNode, bool> nodeFilter = null)
        {
            _client = new HttpClient();
            _tokeniser = tokeniser ?? new Tokeniser();
            _encoding = encoding ?? Encoding.UTF8;
            _mimeType = mimeType;
            _nodeFilter = nodeFilter ?? HtmlTextNodeFilter.Filter;
        }

        public async Task Read(Uri uri, Func<Tuple<Uri, IEnumerable<IToken>>, bool> tokenProcessor)
        {
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

                    if (tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, ExtractTokens(doc.Root))))
                    {

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

                            if (tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, elements.SelectMany(e => ExtractTokens(e)))))
                            {

                            }
                        }
                        else
                        {
                            if (mimeType == TextMimeType.Plain)
                            {
                                if (tokenProcessor(new Tuple<Uri, IEnumerable<IToken>>(uri, _tokeniser.Tokenise(reader.ReadToEnd()))))
                                {

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

        private IEnumerable<IToken> ExtractTokens(XNode doc)
        {
            foreach (var node in AllText(doc))
            {
                if (_nodeFilter(node))
                {
                    foreach (var token in _tokeniser.Tokenise(node.Value))
                    {
                        yield return token;
                    }
                }
            }
        }

        private IEnumerable<XText> AllText(XNode currentNode)
        {
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
