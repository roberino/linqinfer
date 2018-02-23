using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class NetworkSpecificationTests
    {
        [Test]
        public void ToVectorDocument_WhenGivenSpec_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var doc = spec.ToVectorDocument();

            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Children.Count, Is.EqualTo(2));
        }

        [Test]
        public void ToVectorDocument_WhenGivenSingleLayerSpec_ThenCorrectInputVectorSizeReturnedWhenImported()
        {
            var spec = new NetworkSpecification(new LearningParameters(),
                  16,
                  new LayerSpecification(4,
                  Activators.Threshold(),
                  LossFunctions.CrossEntropy,
                  new Range()));

            var doc = spec.ToVectorDocument();

            var spec2 = NetworkSpecification.FromVectorDocument(doc);

            Assert.That(spec2.InputVectorSize, Is.EqualTo(16));
            Assert.That(spec2.Layers.Single().LayerSize, Is.EqualTo(4));
        }

        [Test]
        public void ToVectorDocument_WhenGivenSpecWithOutputTransformation_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var transform = new SerialisableVectorTransformation(new Matrix(new[] { new[] { 1d, 6d, 5d }, new[] { 11d, 23d, 123.3d } }));

            spec.Layers.Last().OutputTransformation = transform;

            var doc = spec.ToVectorDocument();

            var spec2 = NetworkSpecification.FromVectorDocument(doc);
            var spec2transform = spec2.Layers.Last().OutputTransformation;

            Assert.IsNotNull(spec2transform);

            Assert.True(transform.Equals(spec2transform));
        }

        [Test]
        public void ToVectorDoc_WhenGivenSpec_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var doc = spec.ToVectorDocument();

            var spec2 = NetworkSpecification.FromVectorDocument(doc);

            Assert.That(spec2.InputVectorSize, Is.EqualTo(spec.InputVectorSize));
            Assert.That(spec2.Layers.Count, Is.EqualTo(spec.Layers.Count));
            Assert.That(spec2.LearningParameters.LearningRate, Is.EqualTo(spec.LearningParameters.LearningRate));
            Assert.That(spec2.LearningParameters.MinimumError, Is.EqualTo(spec.LearningParameters.MinimumError));
            Assert.That(spec2.OutputVectorSize, Is.EqualTo(spec.OutputVectorSize));

            int i = 0;
            foreach (var layer in spec2.Layers)
            {
                Assert.That(layer.LayerSize, Is.EqualTo(spec.Layers[i].LayerSize));
                Assert.That(layer.InitialWeightRange, Is.EqualTo(spec.Layers[i].InitialWeightRange));
                Assert.That(layer.Activator.Name, Is.EqualTo(spec.Layers[i].Activator.Name));
                Assert.That(layer.Activator.Parameter, Is.EqualTo(spec.Layers[i].Activator.Parameter));

                i++;
            }
        }

        private NetworkSpecification CreateSut()
        {
            var layer1 = new LayerSpecification(4, Activators.Sigmoid(), LossFunctions.Default, new Range(0.4, -0.3));
            var layer2 = new LayerSpecification(2, Activators.Sigmoid(), LossFunctions.CrossEntropy, new Range(0.4, -0.3));
            var spec = new NetworkSpecification(new LearningParameters(), layer1, layer2);

            spec.LearningParameters.MinimumError = 0.999;
            spec.LearningParameters.LearningRate = 0.222;

            return spec;
        }
    }
}