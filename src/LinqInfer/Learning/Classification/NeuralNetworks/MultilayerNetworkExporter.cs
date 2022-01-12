using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using System.Linq;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    class MultilayerNetworkExporter
    {
        /// <summary>
        /// Exports the raw data
        /// </summary>
        public PortableDataDocument Export(MultilayerNetwork network)
        {
            var doc = new PortableDataDocument();

            foreach (var prop in network.Properties)
            {
                doc.Properties["_" + prop.Key] = prop.Value;
            }

            doc.Properties["Label"] = "Network";

            doc.WriteChildObject(network.Specification);

            var idTracker = new HashSet<string>();

            network.ForwardPropagate(x =>
            {
                if (!idTracker.Contains(x.Id))
                {
                    var data = x.ExportData();

                    data.Properties["Label"] = x.ToString();

                    doc.Children.Add(data);

                    idTracker.Add(x.Id);
                }
            });

            return doc;
        }

        public MultilayerNetwork Import(PortableDataDocument doc)
        {
            return CreateFromVectorDocument(doc);
        }

        static MultilayerNetwork CreateFromVectorDocument(PortableDataDocument doc)
        {
            var spec = NetworkSpecification.FromDataDocument(doc.Children.First());
            var properties = doc.Properties.Where(p => p.Key.StartsWith("_")).ToDictionary(p => p.Key.Substring(1), p => p.Value);

            var network = new MultilayerNetwork(spec, properties);

            network.ForwardPropagate(x =>
            {
                var query = doc.QueryChildren(new
                {
                    x.Id
                }).ToList();

                if (query.Count > 1)
                {
                    ArgAssert.AssertEquals(query.Count, 1, $"Multiple ids found for {x.Id}");
                }

                var layerData = query.Single();

                x.ImportData(layerData);
            });

            return network;
        }
    }
}