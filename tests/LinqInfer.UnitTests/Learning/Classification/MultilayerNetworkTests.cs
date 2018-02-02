using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning.Classification
{
    [TestFixture]
    public class MultilayerNetworkTests : TestFixtureBase
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

        [Test]
        public async Task ExportNetworkTopologyAsync()
        {
            var parameters = new NetworkParameters(new int[] { 2, 8, 4 });
            var network = new MultilayerNetwork(parameters);

            network.ForEachLayer(l =>
            {
                l.ForEachNeuron((n, i) =>
                {
                    n.Adjust((w, k) => w * 0.1);

                    return 0d;
                });

                return 1;
            });

            var topology = await network.ExportNetworkTopologyAsync();

            var xml = await topology.ExportAsGexfAsync();

            LogVerbose(xml.ToString());
        }

        [Test]
        public void Save_And_Load()
        {
            var parameters = new NetworkParameters(new int[] { 2, 8, 4 })
            {
                LearningRate = 0.123
            };

            var network = new MultilayerNetwork(parameters);

            {
                int li = 0;

                network.ForEachLayer(l =>
                {
                    li += 10;

                    l.ForEachNeuron((n, i) =>
                    {
                        n.Adjust((w, wi) => wi * li);
                        return 0;
                    }).ToList();
                    return 0;
                }).ToList();
            }

            byte[] serialisedData;

            using (var blobStore = new MemoryStream())
            {
                network.Save(blobStore);

                blobStore.Flush();

                serialisedData = blobStore.ToArray();
            }

            using (var blobStoreRead = new MemoryStream(serialisedData))
            {
                var network2 = MultilayerNetwork.LoadData(blobStoreRead);

                Assert.That(network2.Parameters.LearningRate, Is.EqualTo(0.123));
                Assert.That(network2.Parameters.InputVectorSize, Is.EqualTo(2));
                Assert.That(network2.Parameters.OutputVectorSize, Is.EqualTo(4));
                Assert.That(network2.Layers.Count(), Is.EqualTo(3));

                int li = 0;

                network2.ForEachLayer(l =>
                {
                    li += 10;

                    l.ForEachNeuron((n, i) =>
                    {
                        n.Adjust((w, wi) =>
                        {
                            Assert.That(w == wi * li);
                            return w;
                        });
                        return 0;
                    }).ToList();
                    return 0;
                }).ToList();
            }
        }
    }
}