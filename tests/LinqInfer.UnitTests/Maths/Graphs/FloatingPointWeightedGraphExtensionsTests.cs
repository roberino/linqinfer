using LinqInfer.Maths.Graphs;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Maths.Graphs
{
    [TestFixture]
    public class FloatingPointWeightedGraphExtensionsTests : TestFixtureBase
    {
        [Test]
        public async Task GetAdjacencyMatrix_ReturnsExpectedWeights()
        {
            var graph = new WeightedGraph<char, double>(new WeightedGraphInMemoryStore<char, double>(), (x, y) => x + y);

            var a = await graph.FindOrCreateVertexAsync('a');
            var b = await a.ConnectToAsync('b', 1);
            var c = await graph.FindOrCreateVertexAsync('c');
            var d = await c.ConnectToAsync('d', 2);

            await b.ConnectToAsync(c, 1);

            await graph.SaveAsync();

            var m = await graph.GetAdjacencyMatrix();

            Assert.That(m.Height, Is.EqualTo(4));
            Assert.That(m.Width, Is.EqualTo(4));
            Assert.That(m[m.LabelIndexes['a'], m.LabelIndexes['b']], Is.EqualTo(1));
            Assert.That(m[m.LabelIndexes['c'], m.LabelIndexes['d']], Is.EqualTo(2));
            Assert.That(m[m.LabelIndexes['b'], m.LabelIndexes['d']], Is.EqualTo(0));
        }

        [Test]
        public async Task VertexCosineSimilarity_ReturnsExpectedValue()
        {
            var graph = new WeightedGraph<char, double>(new WeightedGraphInMemoryStore<char, double>(), (x, y) => x + y);

            var a = await graph.FindOrCreateVertexAsync('a');
            var b = await a.ConnectToAsync('b', 1);

            var c = await graph.FindOrCreateVertexAsync('c');

            await c.ConnectToAsync(b, 1);

            var d = await graph.FindOrCreateVertexAsync('d');

            await d.ConnectToAsync(b, 1);
            await d.ConnectToAsync(c, 1);

            await graph.SaveAsync();

            var cosineAB = await a.VertexCosineSimilarityAsync(b);
            var cosineAC = await a.VertexCosineSimilarityAsync(c);
            var cosineDA = await d.VertexCosineSimilarityAsync(a);

            Assert.That(cosineAB, Is.EqualTo(0));
            Assert.That(cosineAC, Is.EqualTo(1));
            Assert.That(cosineDA, IsAround(0.7071));
        }
    }
}