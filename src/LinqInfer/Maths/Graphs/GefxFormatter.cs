using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Maths.Graphs
{
    internal class GefxFormatter
    {
        public const string GFXNamespace = "http://www.gexf.net/1.2draft";
        public const string GFXVisualisationNamespace = "http://www.gexf.net/1.1draft/viz";
        public const string XSINamespace = "http://www.w3.org/2001/XMLSchema-instance";

        public async Task<XDocument> FormatAsync<T, C>(WeightedGraph<T, C> graph)
            where T : IEquatable<T>
            where C : IComparable<C>
        {
            var doc = new XDocument(new XElement(XName.Get("gexf", GFXNamespace)));
            var graphNode = new XElement(XName.Get("graph", GFXNamespace), new XAttribute("mode", "static"), new XAttribute("defaultedgetype", "directed")); // <graph mode="static" defaultedgetype="directed">
            var nodes = new XElement(XName.Get("nodes", GFXNamespace));
            var edges = new XElement(XName.Get("edges", GFXNamespace));

            doc.Root.SetAttributeValue(XNamespace.Xmlns + "viz", GFXVisualisationNamespace);
            doc.Root.SetAttributeValue(XNamespace.Xmlns + "xsi", XSINamespace);

            doc.Root.Add(graphNode);

            var allEdges = new List<Tuple<int, C, T>>();
            var idLookup = new Dictionary<T, int>();
            var attribLookup = new Dictionary<string, Tuple<TypeCode, int>>();

            int i = 1;
            int a = 1;

            foreach (var vertex in await graph.FindAllVertexesAsync())
            {
                idLookup[vertex.Label] = i;

                var node = new XElement(XName.Get("node", GFXNamespace),
                    new XAttribute("id", i),
                    new XAttribute("label", vertex.Label));

                nodes.Add(node);

                var attribs = await vertex.GetAttributesAsync();

                if (attribs.Any())
                {
                    bool hasPlainAttribs = false;

                    var attrsNode = new XElement(XName.Get("attvalues", GFXNamespace));

                    foreach (var attr in attribs.Where(ak => !ak.Key.StartsWith("viz:")))
                    {
                        Tuple<TypeCode, int> ameta;

                        if (!attribLookup.TryGetValue(attr.Key, out ameta))
                        {
                            attribLookup[attr.Key] = ameta = new Tuple<TypeCode, int>(Type.GetTypeCode(attr.Value.GetType()), a++);
                        }

                        attrsNode.Add(
                            new XElement(XName.Get("attvalues", GFXNamespace),
                            new XAttribute("for", ameta.Item2),
                            new XAttribute("value", attr.Value)));

                        hasPlainAttribs = true;
                    }

                    if (hasPlainAttribs) node.Add(attrsNode);

                    foreach (var visualProp in attribs.Where(ak => ak.Key.StartsWith("viz:"))
                        .Select(ax => new
                        {
                            parts = ax.Key.Split(':').SelectMany(p => p.Split('.')).ToList(),
                            val = ax.Value
                        })
                        .Select(ax => new
                        {
                            ns = ax.parts[0],
                            name = ax.parts[1],
                            path = ax.parts.Count > 2 ? ax.parts[2] : ax.parts[1],
                            val = ax.val
                        })
                        .GroupBy(ax => ax.name)
                    )
                    {
                        var vnode = new XElement(XName.Get(visualProp.Key, GFXVisualisationNamespace));

                        foreach (var g in visualProp)
                        {
                            vnode.SetAttributeValue(g.path, g.val);
                        }

                        node.Add(vnode);
                    }
                }

                foreach (var edge in await vertex.GetEdgesAsync())
                {
                    allEdges.Add(new Tuple<int, C, T>(i, edge.Weight, edge.Value.Label));
                }

                i++;
            }

            if (attribLookup.Any())
            {
                var attrLabelsNode = new XElement(XName.Get("attributes", GFXNamespace), new XAttribute("class", "node"));

                foreach(var kv in attribLookup)
                {
                    attrLabelsNode.Add(new XElement(XName.Get("attribute", GFXNamespace),
                        new XAttribute("id", kv.Value.Item2),
                        new XAttribute("title", kv.Key),
                        new XAttribute("type", TranslateType(kv.Value.Item1))));
                }

                graphNode.Add(attrLabelsNode);
            }

            graphNode.Add(nodes);

            //  <edge id="0" source="b" target="e" />

            i = 1;

            foreach (var edge in allEdges)
            {
                var edgeNode = new XElement(XName.Get("edge", GFXNamespace),
                    new XAttribute("id", i++),
                    new XAttribute("source", edge.Item1),
                    new XAttribute("target", idLookup[edge.Item3]),
                    new XAttribute("weight", edge.Item2));

                edges.Add(edgeNode);
            }

            graphNode.Add(edges);

            return doc;
        }

        private string TranslateType(TypeCode typeCode)
        {
            switch (typeCode)
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return "integer";
                case TypeCode.Single:
                case TypeCode.Double:
                    return "float";
                default:
                    return typeCode.ToString().ToLower();
            }
        }
    }
}