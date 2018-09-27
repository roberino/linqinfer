using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    sealed class DefaultContentReader : IContentReader
    {
        readonly ITokeniser _tokeniser;
        readonly Func<XNode, bool> _nodeFilter;
        readonly Func<XElement, IEnumerable<string>> _linkExtractor;

        public DefaultContentReader(
            ITokeniser tokeniser = null,
            Func<XNode, bool> nodeFilter = null,
            Func<XElement, IEnumerable<string>> linkExtractor = null)
        {
            _tokeniser = tokeniser ?? new Tokeniser();
            _nodeFilter = nodeFilter ?? HtmlTextNodeFilter.Filter;
            _linkExtractor = linkExtractor ?? (e => e.Descendants().Where(x => x.Name.LocalName == "a").Select(a => a.Attribute("href")).Where(a => a != null).Select(a => a.Value));
        }

        public Task<HttpDocument> ReadAsync(
            Uri uri,
            Stream content, 
            IDictionary<string, string[]> headers,
            string mimeType,
            Encoding encoding,
            Func<XElement, XElement> targetElement)
        {
            HttpDocument result;

            var knownMimeType = TextMimeTypeParser.Parse(mimeType);

            using (var reader = new StreamReader(content, encoding))
            {
                switch (knownMimeType)
                {
                    case TextMimeType.Plain:
                        result = ReadText(uri, reader, headers);
                        break;
                    case TextMimeType.Xml:
                        result = ReadXml(uri, targetElement, reader, headers);
                        break;
                    case TextMimeType.Html:
                        result = ReadHtml(uri, targetElement, reader, headers);
                        break;
                    default:
                        result = new HttpDocument(uri, Enumerable.Empty<IToken>(), Enumerable.Empty<RelativeLink>(), headers);
                        break;
                }
            }

            return Task.FromResult(result);
        }

        HttpDocument ReadXml(
            Uri uri,
            Func<XElement, XElement> targetElement,
            TextReader reader,
            IDictionary<string, string[]> headers)
        {
            var doc = XDocument.Load(reader);

            XElement root = targetElement == null ? doc.Root : targetElement(doc.Root);

            if (root != null)
            {
                var links = _linkExtractor(doc.Root);

                return new HttpDocument(uri, ExtractTokens(root), links.Select(l => CreateRelLink(uri, l)).Distinct().ToList(), headers);
            }

            return new HttpDocument(uri, Enumerable.Empty<IToken>(), Enumerable.Empty<RelativeLink>(), headers);
        }

        HttpDocument ReadText(Uri uri,
            TextReader reader,
            IDictionary<string, string[]> headers)
        {
            var docContent = reader.ReadToEnd();

            return new HttpDocument(uri, _tokeniser.Tokenise(docContent), Enumerable.Empty<RelativeLink>(), headers);
        }

        HttpDocument ReadHtml(
            Uri uri, 
            Func<XElement, XElement> targetElement,
            TextReader reader, 
            IDictionary<string, string[]> headers)
        {
            var parser = new HtmlParser();

            var elements = parser.Parse(reader);

            var head = (elements.FirstOrDefault(e => e.NodeType == System.Xml.XmlNodeType.Element) as XElement)?.Elements(XName.Get("head"));
            var title = head?.Elements("title").FirstOrDefault();

            var selected = targetElement != null ? elements.Where(e => e.NodeType == System.Xml.XmlNodeType.Element).Select(e => targetElement((XElement)e)) : elements;

            {
                var links = selected.Where(x => x != null && x.NodeType == System.Xml.XmlNodeType.Element).Cast<XElement>().SelectMany(x => _linkExtractor(x));

                var httpDoc = new HttpDocument(
                    uri,
                    selected.SelectMany(e => ExtractTokens(e)),
                    links
                        .Select(l => CreateRelLink(uri, l))
                        .Distinct()
                        .ToList(),
                        headers
                        );

                if (head != null)
                {
                    if (title != null)
                    {
                        httpDoc.Title = title.Value;
                    }
                }

                return httpDoc;
            }
        }

        RelativeLink CreateRelLink(Uri baseUri, string relLink, string relationship = "link")
        {
            if (relLink == null) return new RelativeLink() { Url = baseUri, Rel = "self" };

            var hash = relLink.IndexOf('#');

            return new RelativeLink() { Url = new Uri(baseUri, (hash > -1) ? relLink.Substring(0, hash) : relLink), Rel = relationship };
        }

        IEnumerable<IToken> ExtractTokens(XNode doc)
        {
            return XmlTextExtractor.ExtractTokens(doc, _nodeFilter, _tokeniser);
        }
    }
}