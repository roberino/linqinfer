using System.IO;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class BackPropagationLearningTests
    {
        [TestCase(2, 0)]
        [TestCase(4, 0)]
        [TestCase(6, 2)]
        public void InitialiseAndTrain_ReturnsErrorGt0(int layer1Size, int layer2Size)
        {
            var parameters = new NetworkParameters(new int[] { 4, layer1Size, layer2Size, 4 });

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
            var parameters = new NetworkParameters(new[] { 4, 4 });
            var network = new MultilayerNetwork(parameters);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_MultilayerAndNeuronCounts_ReturnsErrorGt0()
        {
            var parameters = new NetworkParameters(new[] { 4, 2, 4 });
            var network = new MultilayerNetwork(parameters);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_IsTrained()
        {
            var parameters = new NetworkParameters(new[] { 4, 2, 4 });
            var network = new MultilayerNetwork(parameters);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_SaveThenLoad()
        {
            var parameters = new NetworkParameters(new[] { 4, 3, 4 });
            var network = new MultilayerNetwork(parameters);

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            using (var ms = new MemoryStream())
            {
                network.Save(ms);

                ms.Position = 0;

                var network2 = MultilayerNetwork.LoadData(ms);

                Assert.That(network2 != null);

                var output2 = network2.Evaluate(ColumnVector1D.Create(0.1, 0.3, 0.2, 0.1));

                Assert.That(output2 != null);
            }
        }
    }
}