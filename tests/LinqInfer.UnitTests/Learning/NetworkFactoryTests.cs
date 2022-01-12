using LinqInfer.Learning;
using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class NetworkFactoryTests
    {
        [Test]
        public void CreateNetworkFactory_FromExpression_ReturnsValidNetwork()
        {
            var fact = NetworkFactory.CreateNetworkFactory<string>(x => new OneOfNVector(10, x.Length), 10);

            var network = fact.CreateConvolutionalNetwork<string>(10, 4, lc =>
            {
                lc.LearningRate = 0.1;
                lc.Momentum = 0.01;
            });

            network.Train("1", "a");
            network.Train("12", "b");
            network.Train("123", "c");

            var results = network.Classify("1");

            Assert.That(results.Any());
        }

        [Test]
        public void CreateNetworkFactory_FromObjectType_ReturnsValidNetwork()
        {
            var fact = NetworkFactory.CreateNetworkFactory<TestData.Pirate>();

            var network = fact.CreateConvolutionalNetwork<string>(10);

            foreach (var item in TestData.CreatePirates())
            {
                network.Train(item, item.Category);
            }

            var results = network.Classify(new TestData.Pirate()
            {
                Age = 67,
                Gold = 33,
                Ships = 44
            });

            Assert.That(results.Any());
        }

        [Test]
        public void CreateCategoricalNetworkFactory_CreateConvolutionalNetworkFromStrings_ReturnsValidNetwork()
        {
            var fact = NetworkFactory.CreateCategoricalNetworkFactory<string>(4);

            var network = fact.CreateConvolutionalNetwork<string>(10);

            network.Train("a", "a");
            network.Train("b", "b");
            network.Train("c", "c");

            var results = network.Classify("b");

            Assert.That(results.Any());
        }

        [Test]
        public void CreateCategoricalNetworkFactory_CreateTimeSequenceAnalyser_ReturnsValidNetwork()
        {
            var fact = NetworkFactory.CreateCategoricalNetworkFactory<string>(4);

            var network = fact.CreateTimeSequenceAnalyser();

            network.Train(new[] {"a", "b", "c", "d"});

            var results = network.Simulate("b");

            Assert.That(results.Any());
        }
    }
}