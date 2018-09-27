using System.Threading.Tasks;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths.Graphs;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths.Graphs
{
    [TestFixture]
    public class SpatialExtensionTests : TestFixtureBase
    {
        [Test]
        public async Task SetAttributes_StoresAttribAsExpected()
        {
            var storage = new WeightedGraphInMemoryStore<string, int>();
            var graph = new WeightedGraph<string, int>(storage, (x, y) => x + y);

            var a = await graph.FindOrCreateVertexAsync("a");
            var b = await a.ConnectToAsync("b", 5);
            var c = await a.ConnectToAsync("c", 5);

            await a.SetPositionAndSizeAsync(new Point3D() { X = 15, Y = 12, Z = 1 });
            await b.SetPositionAndSizeAsync(new Point3D() { X = 3, Y = 1, Z = 8 });
            await c.SetPositionAndSizeAsync(new Point3D() { X = 6, Y = 5, Z = 4 });

            await graph.FitWithinRectangle(new Point3D(), new Point3D() { X = 5, Y = 6, Z = 7 });

            var aAttribs = await a.GetAttributesAsync();
            var bAttribs = await b.GetAttributesAsync();
            var cAttribs = await c.GetAttributesAsync();

            Assert.That(aAttribs["viz:position.x"], Is.EqualTo(5));
            Assert.That(aAttribs["viz:position.y"], Is.EqualTo(6));
            Assert.That(aAttribs["viz:position.z"], Is.EqualTo(0));

            Assert.That(bAttribs["viz:position.x"], Is.EqualTo(0));
            Assert.That(bAttribs["viz:position.y"], Is.EqualTo(0));
            Assert.That(bAttribs["viz:position.z"], Is.EqualTo(7));

            Assert.That(cAttribs["viz:position.x"], Is.EqualTo(1d / 4d * 5d));
            Assert.That(cAttribs["viz:position.y"], Is.EqualTo(4d / 11d * 6d));
            Assert.That(cAttribs["viz:position.z"], Is.EqualTo(3d / 7d * 7d));
        }
    }
}