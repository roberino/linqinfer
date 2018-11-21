using LinqInfer.Data.Serialisation;
using System.Linq;

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

            network.ForwardPropagate(x =>
            {
                var data = x.ExportData();

                data.Properties["Label"] = x.ToString();

                doc.Children.Add(data);
            });

            return doc;
        }

        public MultilayerNetwork Import(PortableDataDocument doc)
        {
            return CreateFromVectorDocument(doc);
        }

        static MultilayerNetwork CreateFromVectorDocument(PortableDataDocument doc)
        {
            var spec = NetworkSpecification.FromVectorDocument(doc.Children.First());
            var properties = doc.Properties.Where(p => p.Key.StartsWith("_")).ToDictionary(p => p.Key.Substring(1), p => p.Value);

            var network = new MultilayerNetwork(spec, properties);

            network.ForwardPropagate(x =>
            {
                var layerData = doc.Children.Single(c => c.Properties[nameof(INetworkSignalFilter.Id)] == x.Id);

                x.ImportData(layerData);
            });

            return network;
        }
    }
}