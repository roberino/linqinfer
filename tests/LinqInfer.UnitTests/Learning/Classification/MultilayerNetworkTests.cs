using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class MultilayerNetworkTests : TestFixtureBase
    {
        [Test]
        public void WhenDataExport_TheMatrixReturnedPerLayer()
        {
            var parameters = new NetworkSpecification(8, new NetworkLayerSpecification(1, 4));
            var network = SetupTestNetwork(parameters);

            var weights = network.ExportRawData().ToList();

            Assert.That(weights.Count, Is.EqualTo(1));
            Assert.That(weights[0].Width, Is.EqualTo(9)); // input size + bias
            Assert.That(weights[0].Height, Is.EqualTo(4));
        }

        [Test]
        public async Task ExportNetworkTopologyAsync()
        {
            var parameters = new NetworkSpecification(2, new NetworkLayerSpecification(1, 4));
            var network = new MultilayerNetwork(parameters);

            network.ForwardPropagate(m =>
            {
                if (m is ILayer l)
                {
                    l.ForEachNeuron((n, i) =>
                    {
                        n.Adjust((w, k) => w * 0.1);

                        return 0d;
                    });
                }
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
                LossFunctions.Square,
                new NetworkLayerSpecification(1, 4,
                Activators.Threshold(),
                WeightUpdateRules.Default(),
                new Range()));

            var attribs = new Dictionary<string, string>()
            {
                ["x"] = "123"
            };

            var network = new MultilayerNetwork(spec, attribs);

            var doc = network.ExportData();

            Console.Write(doc.ExportAsXml().ToString());

            var network2 = MultilayerNetwork.CreateFromData(doc);

            Assert.That(network.Specification.Equals(network2.Specification));
        }

        MultilayerNetwork SetupTestNetwork(NetworkSpecification specification)
        {
            var network = new MultilayerNetwork(specification);

            AdjustData(network);

            return network;
        }

        void AdjustData(MultilayerNetwork network)
        {
            network.ForwardPropagate(m =>
            {
                if (m is ILayer l)
                {
                    l.ForEachNeuron((n, i) =>
                    {
                        n.Adjust((w, k) => w * 0.1);

                        return 0d;
                    });
                }
            });
        }
    }
}