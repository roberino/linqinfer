using System.Collections.Generic;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    internal static class HtmlTextNodeFilter
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
