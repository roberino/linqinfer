using LinqInfer.Learning.Classification;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Learning.Classification
{
    [TestFixture]
    public class MultilayerNetworkTests
    {
        [Test]
        public void CreateNewInstance_IsCorrectlyInitialised()
        {
            var parameters = new NetworkParameters(new int[] { 2, 8, 4 });
            var network = new MultilayerNetwork(parameters);

            Assert.That(network.Parameters, Is.SameAs(parameters));
            Assert.That(network.LastLayer, Is.SameAs(network.Layers.Last()));
            //Assert.That(network.Layers.Count(), Is.EqualTo(2));

            network.ForEachLayer(l =>
            {
                l.ForEachNeuron((n, i) =>
                {   
                    n.Adjust((w, k) => w * 0.1);

                    return 0d;
                });

                return 1;
            });

            network.PruneInputs(1);

            Assert.That(network.Parameters.InputVectorSize, Is.EqualTo(1));
        }
    }
}
