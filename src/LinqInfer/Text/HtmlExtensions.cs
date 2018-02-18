using LinqInfer.Data;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Features;
using LinqInfer.Text.Http;
using LinqInfer.Text.VectorExtraction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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
        /// Parses a stream as a HTML document, converting it into an <see cref="XDocument"/>
        /// </summary>
        /// <param name="stream">The HTML stream</param>
        /// <param name="encoding">The text encoding</param>
        /// <returns>An <see cref="XDocument"/></returns>
        public static XDocument OpenAsHtmlDocument(this Stream stream, Encoding encoding = null)
        {
            using (var reader = encoding == null ? new StreamReader(stream, true) : new StreamReader(stream, encoding))
            {
                var nodes = new HtmlParser(true).Parse(reader).ToList();

                if (nodes.Count == 1 && nodes.Single().NodeType == System.Xml.XmlNodeType.Element)
                {
                    return new XDocument(nodes.Single());
                }
                else
                {
                    return new XDocument(new XElement("html", new XElement("body", nodes)));
                }
            }
        }
    }
}
