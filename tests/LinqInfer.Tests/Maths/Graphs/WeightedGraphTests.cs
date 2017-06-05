using LinqInfer.Maths;
using LinqInfer.Maths.Graphs;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

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
        public async Task Export_CreatesExpectedXmlStructure()
        {
            var graph = new WeightedGraph<string, int>(new WeightedGraphInMemoryStore<string, int>(), (x, y) => x + y);

            var root = await graph.FindOrCreateVertexAsync("a");

            var attribs = await root.GetAttributesAsync();

            attribs["x"] = 123;

            await root.ConnectToAsync("b", 154);

            await graph.SaveAsync();

            var xml = await graph.ExportAsGefxAsync();

            var xml2 = GetResourceAsXml("gexf.xml");

            Console.WriteLine(xml);

            Assert.That(xml.ToString(), Is.EqualTo(xml2.ToString()));
        }
    }
}