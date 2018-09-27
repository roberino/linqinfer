using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    static class XmlTextExtractor
    {
        public static IEnumerable<IToken> ExtractTokens(XNode doc, Func<XNode, bool> nodeFilter, ITokeniser tokeniser)
        {
            foreach (var node in ExtractAllText(doc))
            {
                if (nodeFilter(node))
                {
                    foreach (var token in tokeniser.Tokenise(node.Value))
                    {
                        if (token is Token)
                        {
                            ((Token)token).Weight = GetTokenWeight(node);
                        }

                        yield return token;
                    }
                }
            }
        }
        public static IEnumerable<XText> ExtractAllText(XNode currentNode)
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
                        foreach (var text in ExtractAllText((XElement)node))
                        {
                            yield return text;
                        }
                    }
                }
            }
        }

        public static byte GetTokenWeight(XText text)
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
    }
}