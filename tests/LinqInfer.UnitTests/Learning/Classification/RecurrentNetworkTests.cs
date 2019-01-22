using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class RecurrentNetworkTests
    {
        [Test]
        public void AddModule_ComplexConfiguration_ReturnsSpec()
        {
            var builder = SetupLstmNetwork();

            var spec = builder.Build();

            Assert.That(spec, Is.Not.Null);
        }

        [Test]
        public void SimpleConfiguration_BuildAndTrain_ReturnsNonZeroError()
        {
            var builder = RecurrentNetworkBuilder.Create(4);

            var spec = builder.ConfigureModules(mb =>
                {
                    var layer1 = mb.Layer(4, Activators.Sigmoid());
                    var layer2 = mb.Layer(2, Activators.Sigmoid());

                    layer1.ConnectTo(layer2);
                    layer1.ReceiveFrom(layer2);

                    return mb.Output(layer2);
                })
                .Build();

            var err = spec.Train(ColumnVector1D.Create(1, 2, 3, 4), ColumnVector1D.Create(1, 2));

            Assert.That(err, Is.GreaterThan(0));
        }

        [Test]
        public void Lstm_BuildAndTrain_ReturnsNonZeroError()
        {
            var builder = SetupLstmNetwork();

            var spec = builder.Build();

            var err = spec.Train(new Vector(1d, 2d, 3d, 4d), new OneOfNVector(2, 1));

            Assert.That(err, Is.GreaterThan(0));
        }

        public INetworkBuilder SetupLstmNetwork(int inputSize = 4, int outputSize = 2)
        {
            var builder = RecurrentNetworkBuilder.Create(inputSize);

            return builder.ConfigureLongShortTermMemoryNetwork(outputSize);
        }
    }
}