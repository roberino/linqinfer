using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class NetworkSpecificationTests
    {
        [Test]
        public void ToVectorDocument_WhenGivenSpec_ThenValidDocReturned()
        {
            var layer1 = new LayerSpecification(4, Activators.Sigmoid(), new Range(0.4, -0.3));
            var layer2 = new LayerSpecification(2, Activators.Sigmoid(), new Range(0.4, -0.3));
            var spec = new NetworkSpecification(new LearningParameters(), layer1, layer2);

            var doc = spec.ToVectorDocument();

            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Children.Count, Is.EqualTo(2));
        }
    }
}