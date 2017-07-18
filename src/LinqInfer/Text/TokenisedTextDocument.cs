using System;
using System.Collections.Generic;
using System.Xml.Linq;
using LinqInfer.Data;
using System.Linq;
using LinqInfer.Utility;
using System.Xml;
using System.IO;

namespace LinqInfer.Text
{
    /// <summary>
    /// Represents a document which is an enumeration of text tokens
    /// </summary>
    public class TokenisedTextDocument : IXmlExportable, IXmlImportable
    {
        public TokenisedTextDocument(string id, IEnumerable<IToken> tokens)
        {
            Id = id;
            Tokens = tokens;
            Attributes = CreateAttributeDictionary();
        }

        /// <summary>
        /// The document unique identifier
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// A dictionary of document attributes (only primative types supported)
        /// </summary>
        public IDictionary<string, object> Attributes { get; private set; }

        /// <summary>
        /// The sequence of tokens within the document
        /// </summary>
        public IEnumerable<IToken> Tokens { get; private set; }

        /// <summary>
        /// Exports the content to an XML document
        /// </summary>
        /// <returns></returns>
        public XDocument ExportAsXml()
        {
            var doc = new XDocument(
                new XElement("doc", new XAttribute("id", Id), GetAttributesXml(), new XElement("text", Tokens.Select(t => TokenToNode(t)))));

            return doc;
        }

        /// <summary>
        /// Imports a document from XML, overwriting existing content
        /// </summary>
        public void ImportXml(XDocument xml)
        {
            var tokens = new List<IToken>();
            var attribs = CreateAttributeDictionary();

            foreach (var node in xml.Root.Elements("attributes").Single().Elements())
            {
                attribs[node.Name.LocalName] = node.Value.Parse(node.Attribute("type").Value);
            }

            foreach (var node in xml.Root.Elements("text").Single().Elements("t"))
            {
                tokens.Add(new Token(node.Value, int.Parse(node.Attribute("i").Value), (TokenType)Enum.Parse(typeof(TokenType), node.Attribute("t").Value)));
            }

            Id = xml.Root.Attribute("id").Value;
            Tokens = tokens;
            Attributes = attribs;
        }

        /// <summary>
        /// Returns a document from an XML stream
        /// </summary>
        public static TokenisedTextDocument FromXmlStream(Stream xmlStream)
        {
            var doc = new TokenisedTextDocument("x", Enumerable.Empty<IToken>());
            
            var xml = XDocument.Load(xmlStream);

            doc.ImportXml(xml);

            return doc;
        }

        private XElement GetAttributesXml()
        {
            return new XElement("attributes", Attributes.Select(kv => new XElement(kv.Key, new XAttribute("type", kv.Value.GetTypeCode()), kv.Value)));
        }

        private XNode TokenToNode(IToken token)
        {
            return new XElement("t", new XAttribute("t", token.Type), new XAttribute("w", token.Weight), new XAttribute("i", token.Index), token.Text);
        }

        private IDictionary<string, object> CreateAttributeDictionary()
        {
            var attributes = new ConstrainableDictionary<string, object>();

            attributes.AddContraint((k, v) =>
            {
                if (XmlConvert.VerifyNCName(k) == null) return false;
                return (v.IsPrimativeOrString());
            });

            return attributes;
        }
    }
}