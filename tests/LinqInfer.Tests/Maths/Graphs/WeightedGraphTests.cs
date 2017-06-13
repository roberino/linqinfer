using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Tests.Maths.Graphs
{
    [TestFixture]
    public class WeightedGraphTests : TestFixtureBase
    {
        [Test]
        public async Task SetAttributes_StoresAttribAsExpected()
        {
            var storage = new WeightedGraphInMemoryStore<string, int>();
            var graph = new WeightedGraph<string, int>(storage, (x, y) => x + y);

            var root = await graph.FindOrCreateVertexAsync("a");

            var attribs = await root.GetAttributesAsync();

            attribs["x"] = 123;

            await graph.SaveAsync();

            var rootRef2 = await graph.FindOrCreateVertexAsync("a");

            var attribs2 = await rootRef2.GetAttributesAsync();

            var attribs3 = await storage.GetVertexAttributesAsync("a");

            Assert.That(attribs2["x"], Is.EqualTo(123));
            Assert.That(attribs3["x"], Is.EqualTo(123));
        }

        [Test]
        public async Task SetAttributes_Save_SetAttributes_StoresAttribAsExpected()
        {
            var storage = new WeightedGraphInMemoryStore<string, int>();
            var graph = new WeightedGraph<string, int>(storage, (x, y) => x + y);

            var root = await graph.FindOrCreateVertexAsync("a");

            var attribs = await root.GetAttributesAsync();

            attribs["x"] = 123;

            await graph.SaveAsync();

            var rootRef2 = await graph.FindOrCreateVertexAsync("a");

            var attribs2 = await rootRef2.GetAttributesAsync();
            
            attribs2["x"] = 1234;

            await graph.SaveAsync();

            var attribs3 = await storage.GetVertexAttributesAsync("a");

            Assert.That(attribs3["x"], Is.EqualTo(1234));
        }

        [Test]
        public async Task ConnectToAsync_SetVisualPropsAndExport()
        {
            var graph = new WeightedGraph<string, Fraction>(new WeightedGraphInMemoryStore<string, Fraction>(), (x, y) => x + y);

            var a = await graph.FindOrCreateVertexAsync("a");

            var b = await a.ConnectToAsync("b", Fraction.Half);

            await a.SetColourAsync(23, 28, 55);
            await a.SetPositionAndSizeAsync(10, 14, 16);

            await graph.SaveAsync();

            var xml = await graph.ExportAsGexfAsync();

            Console.WriteLine(xml);

            var allNodes = xml.Root
                .DescendantNodes()
                .Where(n => n.NodeType == System.Xml.XmlNodeType.Element)
                .Cast<XElement>().Where(n => n.Name.LocalName == "node");
            
            var nodea = allNodes.FirstOrDefault(n => n.Attribute("label").Value == "a");
            var nodeb = allNodes.FirstOrDefault(n => n.Attribute("label").Value == "b");

            var colour = nodea.Elements(XName.Get("color", GexfFormatter.GEXFVisualisationNamespace)).FirstOrDefault();
            var pos = nodea.Elements(XName.Get("position", GexfFormatter.GEXFVisualisationNamespace)).FirstOrDefault();

            Assert.That(colour, Is.Not.Null);
            Assert.That(pos, Is.Not.Null);

            Assert.That(colour.Attribute("r").Value, Is.EqualTo("23"));
            Assert.That(colour.Attribute("g").Value, Is.EqualTo("28"));
            Assert.That(colour.Attribute("b").Value, Is.EqualTo("55"));

            Assert.That(pos.Attribute("x").Value, Is.EqualTo("10"));
            Assert.That(pos.Attribute("y").Value, Is.EqualTo("14"));
            Assert.That(pos.Attribute("z").Value, Is.EqualTo("16"));

            Assert.That(nodeb.Elements().Where(e => e.Name.Namespace == GexfFormatter.GEXFVisualisationNamespace).Any(), Is.False);
        }

        [Test]
        public async Task ConnectToAsync_CreatesExpectedWeightedConnections()
        {
            var graph = new WeightedGraph<string, Fraction>(new WeightedGraphInMemoryStore<string, Fraction>(), (x, y) => x + y);

            var a = await graph.FindOrCreateVertexAsync("a");

            var b = await a.ConnectToAsync("b", Fraction.Half);

            var c = await b.ConnectToAsync("c", Fraction.One / 3);

            await c.ConnectToAsync(b, Fraction.Half * 4);

            await graph.SaveAsync();

            var weightAB = await a.GetWeightAsync(b.Label);
            var weightBC = await b.GetWeightAsync(c.Label);
            var weightCB = await c.GetWeightAsync(b.Label);

            Assert.That(weightAB, Is.EqualTo(Fraction.Half));
            Assert.That(weightBC, Is.EqualTo(new Fraction(1, 3)));
            Assert.That(weightCB, Is.EqualTo(2));
        }

        [Test]
        [Category("BuildOmit")]
        public async Task Export_CreatesExpectedXmlStructure()
        {
            var graph = new WeightedGraph<string, int>(new WeightedGraphInMemoryStore<string, int>(), (x, y) => x + y);

            var root = await graph.FindOrCreateVertexAsync("a");

            var attribs = await root.GetAttributesAsync();

            attribs["x"] = 123;

            await root.ConnectToAsync("b", 154);

            await graph.SaveAsync();

            var xml = await graph.ExportAsGexfAsync();

            var xml2 = GetResourceAsXml("gexf.xml");

            xml.Root.Elements().First().Elements().First().RemoveAttributes(); // remove meta for comparison
            xml2.Root.Elements().First().Elements().First().RemoveAttributes();

            Console.WriteLine(xml);

            Assert.That(RemoveWhitespace(xml), Is.EqualTo(RemoveWhitespace(xml2)));
        }
    }
}