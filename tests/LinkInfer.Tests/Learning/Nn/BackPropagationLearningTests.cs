using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System.IO;

namespace LinqInfer.Tests.Learning.Nn
{
    [TestFixture]
    public class BackPropagationLearningTests
    {
        [TestCase(2, 0)]
        [TestCase(4, 0)]
        [TestCase(6, 2)]
        public void InitialiseAndTrain_ReturnsErrorGt0(int layer1Size, int layer2Size)
        {
            var network = new MultilayerNetwork(4);

            network.Initialise(4, layer1Size, layer2Size, 4);

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

        [Test]
        public void Train_Multilayer_SaveThenLoad()
        {
            var network = new MultilayerNetwork(4, new[] { 4, 2 });

            var bp = new BackPropagationLearning(network);

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = bp.Train(input, output);

            using (var ms = new MemoryStream())
            {
                network.Save(ms);

                ms.Position = 0;

                var network2 = MultilayerNetwork.Load(ms);

                Assert.That(network2 != null);

                var output2 = network2.Evaluate(ColumnVector1D.Create(0.1, 0.3, 0.2, 0.1));

                Assert.That(output2 != null);
            }
        }
    }
}