using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class FluentMultilayerNetworkBuilderTests
    {
        [Test]
        public void WhenGivenDefaults_ThenBuildsWithoutError()
        {
            var builder = new FluentMultilayerNetworkBuilder(8, 4);

            var trainingNetwork = builder.Build();

            Assert.That(trainingNetwork, Is.Not.Null);
        }

        [Test]
        public void WhenHiddenSigmoidLayersAdded_ThenBuildsWithoutError()
        {
            var trainingNetwork = new FluentMultilayerNetworkBuilder(8, 4)
                .AddHiddenSigmoidLayer(16)
                .Build();

            Assert.That(trainingNetwork, Is.Not.Null);

            var network = trainingNetwork.Output as MultilayerNetwork;

            Assert.That(network.Specification.Layers.Count, Is.EqualTo(2));
        }

        [Test]
        public void WhenOutputLayerConfigured_ThenActivatorAndLossFunctionCustomised()
        {
            var network = new FluentMultilayerNetworkBuilder(8, 4)
                .ConfigureOutputLayer(Activators.None(), LossFunctions.CrossEntropy)
                .Build()
                .Output as MultilayerNetwork;

            Assert.That(network.Specification.Layers.Count, Is.EqualTo(1));
            Assert.That(network.Specification.Layers.Single().Activator.Name, Is.EqualTo(Activators.Sigmoid().Name));
            Assert.That(network.Specification.Layers.Single().LossFunction, Is.SameAs(LossFunctions.CrossEntropy));
        }
    }
}