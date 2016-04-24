using LinqInfer.Learning.Nn;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Learning.Nn
{
    [TestFixture]
    public class BackPropagationLearningTests
    {
        [Test]
        public void Train_ReturnsErrorGt0()
        {
            var network = new MultilayerNetwork(4);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_ReturnsErrorGt0()
        {
            var network = new MultilayerNetwork(4, new[] { 4, 4 });

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_MultilayerAndNeuronCounts_ReturnsErrorGt0()
        {
            var network = new MultilayerNetwork(4, new[] { 4, 2 });

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_IsTrained()
        {
            var network = new MultilayerNetwork(4, new[] { 4, 2 });

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }
    }
}