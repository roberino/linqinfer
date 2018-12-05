using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class ConvolutionalNetworkBuilderTests
    {
        [Test]
        public void WhenGivenDefaults_ThenBuildsWithoutError()
        {
            var builder = ConvolutionalNetworkBuilder.Create(8, 4);

            var trainingNetwork = builder.ApplyDefaults().Build();

            Assert.That(trainingNetwork, Is.Not.Null);
        }

        [Test]
        public void WhenLearningParamsProvided_ThenSetCorrectlyInSpec()
        {
            var spec = ConvolutionalNetworkBuilder.Create(8, 4)
                .ConfigureLearningParameters(p =>
                {
                    p.LearningRate = 0.12d;
                    p.MinimumError = 0.33d;
                })
                .ConfigureOutput(LossFunctions.Square)
                .Build()
                .Result
                .Specification;

            Assert.That(spec.LearningParameters.LearningRate, Is.EqualTo(0.12d));
            Assert.That(spec.LearningParameters.MinimumError, Is.EqualTo(0.33d));
        }

        [Test]
        public void WhenHiddenSigmoidLayersAdded_ThenBuildsWithoutError()
        {
            var trainingNetwork = ConvolutionalNetworkBuilder.Create(8, 4)
                .AddHiddenSigmoidLayer(16)
                .ConfigureOutput(LossFunctions.Square)
                .Build();

            Assert.That(trainingNetwork, Is.Not.Null);

            var network = trainingNetwork.Output as MultilayerNetwork;

            Assert.That(network.Specification.Modules.Count, Is.EqualTo(2));
        }

        [Test]
        public void WhenOutputLayerConfigured_ThenActivatorAndLossFunctionCustomised()
        {
            var network = ConvolutionalNetworkBuilder.Create(8, 4)
                .ConfigureOutput(LossFunctions.CrossEntropy)
                .Build()
                .Output as MultilayerNetwork;

            Assert.That(network.Specification.Layers.Count, Is.EqualTo(1));
            Assert.That(network.Specification.Layers.Single().Activator.Name, Is.EqualTo(Activators.None().Name));
            Assert.That(network.Specification.Output.LossFunction, Is.SameAs(LossFunctions.CrossEntropy));
        }

        [Test]
        public void WhenOutputTransformationConfigured_ThenActivatorAndLossFunctionCustomised()
        {
            var network = ConvolutionalNetworkBuilder.Create(8, 4)
                .ConfigureOutput(LossFunctions.CrossEntropy, Softmax.Factory)
                .Build()
                .Output as MultilayerNetwork;

            Assert.That(network.Specification.Output.OutputTransformation.GetType().Name, Is.EqualTo(nameof(Softmax)));
        }
    }
}