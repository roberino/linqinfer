using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class BackPropagationLearningTests
    {
        readonly NetworkSpecification _spec = new NetworkSpecification(4,
                new LayerSpecification(2),
                new LayerSpecification(4));

        [TestCase(2, 0)]
        [TestCase(4, 0)]
        [TestCase(6, 2)]
        public void InitialiseAndTrain_ReturnsErrorGt0(int layer1Size, int layer2Size)
        {
            var parameters = new NetworkSpecification(4,
                new LayerSpecification(layer1Size),
                new LayerSpecification(layer2Size),
                new LayerSpecification(4));

            var network = new MultilayerNetwork(parameters);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_ReturnsErrorGt0()
        {
            var network = new MultilayerNetwork(_spec);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_MultilayerAndNeuronCounts_ReturnsErrorGt0()
        {
            var network = new MultilayerNetwork(_spec);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }
    }
}