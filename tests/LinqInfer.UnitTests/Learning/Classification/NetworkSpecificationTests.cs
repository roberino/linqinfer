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

            var doc = spec.ExportData();

            Assert.That(doc, Is.Not.Null);
            Assert.That(doc.Children.Count, Is.EqualTo(3));
        }

        [Test]
        public void FromVectorDocument_WhenGivenExportedData_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var doc = spec.ExportData();

            var spec2 = NetworkSpecification.FromDataDocument(doc);

            Assert.That(spec2, Is.Not.Null);
        }

        [Test]
        public void HaltingFunction_WhenGivenSpec_ThenValidDocReturned()
        {
            var spec = CreateSut();

            spec.TrainingParameters.MinimumError = 0.1;

            var halt = spec.TrainingParameters.EvaluateHaltingFunction(0, 0.09);

            Assert.True(halt);

            spec.TrainingParameters.MinimumError = 0.5;

            halt = spec.TrainingParameters.EvaluateHaltingFunction(0, 0.4);

            Assert.True(halt);
        }

        [Test]
        public void ToVectorDocument_WhenGivenSingleLayerSpec_ThenCorrectInputVectorSizeReturnedWhenImported()
        {
            var spec = new NetworkSpecification(new TrainingParameters(),
                  16,
                LossFunctions.Square,
                  new NetworkLayerSpecification(1, 4,
                  Activators.Threshold(),
                  WeightUpdateRules.Default(),
                  new LinqInfer.Maths.Range()));

            var doc = spec.ExportData();

            var spec2 = NetworkSpecification.FromDataDocument(doc);

            Assert.That(spec2.InputVectorSize, Is.EqualTo(16));
            Assert.That(spec2.Layers.Single().LayerSize, Is.EqualTo(4));
        }

        [Test]
        public void ToVectorDocument_WhenGivenSpecWithOutputTransformation_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var transform = new SerialisableDataTransformation(new
                Matrix(new[] {
                    new[] { 1d, 5d },
                    new[] { 11d, 123.3d } }));

            spec.Output.OutputTransformation = transform;

            var doc = spec.ExportData();

            var spec2 = NetworkSpecification.FromDataDocument(doc);
            var spec2transform = spec2.Output.OutputTransformation;

            Assert.IsNotNull(spec2transform);

            Assert.True(transform.Equals(spec2transform));
        }

        [Test]
        public void ToVectorDoc_WhenGivenSpec_ThenValidDocReturned()
        {
            var spec = CreateSut();

            var doc = spec.ExportData();

            var spec2 = NetworkSpecification.FromDataDocument(doc);

            Assert.That(spec2.InputVectorSize, Is.EqualTo(spec.InputVectorSize));
            Assert.That(spec2.Layers.Count, Is.EqualTo(spec.Modules.Count));
            Assert.That(spec2.TrainingParameters.LearningRate, Is.EqualTo(spec.TrainingParameters.LearningRate));
            Assert.That(spec2.TrainingParameters.MinimumError, Is.EqualTo(spec.TrainingParameters.MinimumError));

            int i = 0;
            foreach (var layer in spec2.Layers)
            {
                Assert.That(layer.LayerSize, Is.EqualTo(spec.Layers.ElementAt(i).LayerSize));
                Assert.That(layer.InitialWeightRange, Is.EqualTo(spec.Layers.ElementAt(i).InitialWeightRange));
                Assert.That(layer.Activator.Name, Is.EqualTo(spec.Layers.ElementAt(i).Activator.Name));

                i++;
            }
        }

        NetworkSpecification CreateSut()
        {
            var layer1 = new NetworkLayerSpecification(1, 4, Activators.Sigmoid(), WeightUpdateRules.Default(), new LinqInfer.Maths.Range(0.4, -0.3));
            var layer2 = new NetworkLayerSpecification(2, 2, Activators.Sigmoid(), WeightUpdateRules.Default(), new LinqInfer.Maths.Range(0.4, -0.3));
            var spec = new NetworkSpecification(new TrainingParameters(), layer1.LayerSize, LossFunctions.CrossEntropy, layer1, layer2);

            spec.TrainingParameters.MinimumError = 0.999;
            spec.TrainingParameters.LearningRate = 0.222;

            return spec;
        }
    }
}