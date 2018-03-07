using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class NetworkLayerTests
    {
        [Test]
        public void WhenConstructed_ThenAttributesSetCorrectly()
        {
            var layer = new NetworkLayer(8, 16, Activators.Sigmoid(1.2), LossFunctions.Square);

            Assert.That(layer.Activator.Name, Is.EqualTo(Activators.Sigmoid().Name));
            Assert.That(layer.Activator.Parameter, Is.EqualTo(1.2));
            Assert.That(layer.LossFunction, Is.EqualTo(LossFunctions.Square));
            Assert.That(layer.Size, Is.EqualTo(16));
            Assert.That(layer.InputVectorSize, Is.EqualTo(8));
        }

        [Test]
        public void WhenProcessed_ThenReturnsCorrectSizeVector()
        {
            var layer = new NetworkLayer(3, 16, Activators.Sigmoid(1.2), LossFunctions.Square);

            var vector = layer.Process(ColumnVector1D.Create(1.6, 8.8, 3.3));

            Assert.That(vector.Size, Is.EqualTo(16));
        }
    }
}