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
        public void AddModule_SimpleConfiguration_Train()
        {
            var builder = new FluentNetworkBuilder(4, 2);

            builder.ConfigureModule(mb =>
            {
                var layer1 = mb.Layer(4, Activators.Sigmoid());
                var layer2 = mb.Layer(2, Activators.Sigmoid());

                layer1.ConnectTo(layer2);
                layer1.ReceiveFrom(layer2);

                mb.Output(layer2);
            });

            var spec = builder.Build();

            spec.Train(ColumnVector1D.Create(1, 2, 3, 4), ColumnVector1D.Create(1, 2));
        }

        [Test]
        public void AddModule_ComplexConfiguration_Train()
        {
            var builder = SetupLstmNetwork();

            var spec = builder.Build();

            var output = spec.Parameters.Apply(ColumnVector1D.Create(1, 2, 3, 4));

            Assert.That(output.Size, Is.EqualTo(2));
            Assert.That(output.Sum, Is.Not.EqualTo(0));
        }

        public FluentNetworkBuilder SetupLstmNetwork()
        {
            var inputSize = 4;
            var outputSize = 2;

            var builder = new FluentNetworkBuilder(inputSize, outputSize);

            builder.ConfigureModule(mb =>
            {
                var module = mb.Module(VectorAggregationType.Concatinate);

                var mult1 = mb.Module(VectorAggregationType.Multiply);
                var mult2 = mb.Module(VectorAggregationType.Multiply);
                var mult3 = mb.Module(VectorAggregationType.Multiply);
                var sum1 = mb.Module(VectorAggregationType.Add);
                var tanop1 = mb.Module(VectorAggregationType.HyperbolicTangent);

                var sig1 = mb.Layer(outputSize, Activators.Sigmoid());
                var sig2 = mb.Layer(outputSize, Activators.Sigmoid());
                var tan1 = mb.Layer(outputSize, Activators.HyperbolicTangent());
                var sig3 = mb.Layer(outputSize, Activators.Sigmoid());

                module.ConnectTo(sig1, sig2, tan1, sig3);

                sig1.ConnectTo(mult1);

                sig2.ConnectTo(mult2);
                tan1.ConnectTo(mult2);

                mult1.ConnectTo(sum1);
                mult2.ConnectTo(sum1);

                sum1.ConnectTo(tanop1);

                tanop1.ConnectTo(mult3);
                sig3.ConnectTo(mult3);

                module.ReceiveFrom(mult3);
                mult1.ReceiveFrom(sum1);

                mb.Output(mult3);
            });

            return builder;
        }
    }
}