using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            var network = SetupTestNetwork(parameters);

            Assert.That(network.Parameters, Is.SameAs(parameters));
            Assert.That(network.LastLayer, Is.SameAs(network.Layers.Last()));

            network.PruneInputs(1);

            Assert.That(network.Parameters.InputVectorSize, Is.EqualTo(1));
        }

        [Test]
        public void WhenDataExport_TheMatrixReturnedPerLayer()
        {
            var parameters = new NetworkSpecification(8, new LayerSpecification(4));
            var network = SetupTestNetwork(parameters);

            var weights = network.ExportData().ToList();

            Assert.That(weights.Count, Is.EqualTo(1));
            Assert.That(weights[0].Width, Is.EqualTo(9)); // input size + bias
            Assert.That(weights[0].Height, Is.EqualTo(4));
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
        public void WhenGivenNetworkFromSpecification_ThenCanExportAndImport()
        {
            var lp = new LearningParameters();
            var spec = new NetworkSpecification(lp,
                16,
                new LayerSpecification(4,
                Activators.Threshold(),
                LossFunctions.CrossEntropy,
                DefaultWeightUpdateRule.Create(),
                new Range()));

            var attribs = new Dictionary<string, string>()
            {
                ["x"] = "123"
            };

            var network = new MultilayerNetwork(spec, attribs);

            var doc = network.ToVectorDocument();

            Console.Write(doc.ExportAsXml().ToString());

            var network2 = MultilayerNetwork.CreateFromVectorDocument(doc);

            Assert.That(network.Parameters, Is.Not.Null);
            Assert.That(network.Specification.Equals(network2.Specification));
        }

        [Test]
        public void WhenGivenNetworkFromParams_ThenCanSaveAndLoad()
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

        private MultilayerNetwork SetupTestNetwork(NetworkSpecification specification)
        {
            var network = new MultilayerNetwork(specification);

            AdjustData(network);

            return network;
        }

        private MultilayerNetwork SetupTestNetwork(NetworkParameters parameters)
        {
            var network = new MultilayerNetwork(parameters);

            AdjustData(network);

            return network;
        }

        private void AdjustData(MultilayerNetwork network)
        {
            network.ForEachLayer(l =>
            {
                l.ForEachNeuron((n, i) =>
                {
                    n.Adjust((w, k) => w * 0.1);

                    return 0d;
                });

                return 1;
            });
        }
    }
}