using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class NetworkParametersTests
    {
        [Test]
        public void ToSpecification_WhenGivenParams_ThenValidSpecReturned()
        {
            var parameters = NetworkParameters.Sigmoidal(4, 5, 6, 2);

            var spec = parameters.ToSpecification();

            Assert.That(spec.LearningParameters, Is.Not.Null);
            Assert.That(spec.InputVectorSize, Is.EqualTo(4));
            Assert.That(spec.OutputVectorSize, Is.EqualTo(2));
            Assert.That(spec.Layers.Count, Is.EqualTo(4));

            Assert.That(spec.Layers[0].LayerSize, Is.EqualTo(4));
            Assert.That(spec.Layers[1].LayerSize, Is.EqualTo(5));
            Assert.That(spec.Layers[2].LayerSize, Is.EqualTo(6));
            Assert.That(spec.Layers[3].LayerSize, Is.EqualTo(2));

            foreach (var layer in spec.Layers)
            {
                Assert.That(layer.Activator, Is.Not.Null);
                Assert.That(layer.Activator, Is.EqualTo(parameters.Activator));
                Assert.That(layer.NeuronFactory, Is.Not.Null);
                Assert.That(layer.InitialWeightRange, Is.EqualTo(parameters.InitialWeightRange));
            }
        }
    }
}
