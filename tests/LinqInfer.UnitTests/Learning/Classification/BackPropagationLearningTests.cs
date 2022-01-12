using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class BackPropagationLearningTests
    {
        readonly IClassifierTrainingContext<INetworkModel> _trainingContext = ConvolutionalNetworkBuilder.Create(4)
            .AddHiddenLayer(2)
            .AddHiddenLayer(4)
            .ConfigureOutput(LossFunctions.Square)
            .Build();

        [TestCase(2, 1)]
        [TestCase(4, 2)]
        [TestCase(6, 2)]
        public void InitialiseAndTrain_ReturnsErrorGt0(int layer1Size, int layer2Size)
        {
            var trainingContext = ConvolutionalNetworkBuilder.Create(4)
                .AddHiddenLayer(layer1Size)
                .AddHiddenLayer(layer2Size)
                .AddHiddenLayer(4)
                .ConfigureOutput(LossFunctions.Square)
                .Build();

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = trainingContext.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void InitialiseAndTrain_ReturnsError()
        {
            var trainingContext = ConvolutionalNetworkBuilder.Create(4, 2)
                .AddHiddenLayer(1)
                .ConfigureOutput(LossFunctions.Square)
                .Build();

            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 1);

            var err = trainingContext.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_Multilayer_ReturnsErrorGt0()
        {
            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = _trainingContext.Train(input, output);

            Assert.That(err > 0);
        }

        [Test]
        public void Train_MultilayerAndNeuronCounts_ReturnsErrorGt0()
        {
            var input = ColumnVector1D.Create(0, 0.2, 0.66, 0.28);
            var output = ColumnVector1D.Create(0, 0, 0, 0.999);

            var err = _trainingContext.Train(input, output);

            Assert.That(err > 0);
        }
    }
}