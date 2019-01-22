using LinqInfer.Text.Html;
using LinqInfer.Text.Http;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text
{
    public static class HtmlExtensions
    {
        public static Task<HttpDocument> OpenAsHtmlTokenDocumentAsync(this Uri rootUri)
        {
            return new HttpDocumentServices().GetDocumentAsync(rootUri);
        }

        /// <summary>
        /// Read a HTML document from the text reader, converting it into an <see cref="XDocument"/>
        /// </summary>
        /// <param name="reader">A text reader</param>
        /// <returns>An <see cref="XDocument"/></returns>
        public static XDocument OpenAsHtmlDocument(this TextReader reader)
        {
            var nodes = new HtmlParser(true).Parse(reader).ToList();

            if (nodes.Count == 1 && nodes.Single().NodeType == System.Xml.XmlNodeType.Element)
            {
                return new XDocument(nodes.Single());
            }

            return new XDocument(new XElement("html", new XElement("body", nodes)));
        }

        /// <summary>
        /// Parses a stream as a HTML document, converting it into an <see cref="XDocument"/>
        /// </summary>
        /// <param name="stream">The HTML stream</param>
        /// <param name="encoding">The text encoding</param>
        /// <returns>An <see cref="XDocument"/></returns>
        public static XDocument OpenAsHtmlDocument(this Stream stream, Encoding encoding = null)
        {
            using (var reader = encoding == null ? new StreamReader(stream, true) : new StreamReader(stream, encoding))
            {
                return reader.OpenAsHtmlDocument();
            }
        }
    }
}