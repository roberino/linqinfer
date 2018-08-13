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

            doc.Version = 2;

            doc.Properties["Label"] = "Network";

            doc.WriteChildObject(network.Specification);

            int i = 0;

            foreach (var layer in network.Layers)
            {
                i++;

                var layerDoc = layer.Export();

                layerDoc.Properties["Label"] = "Layer " + i;
                layerDoc.SetType<NetworkLayer>();

                doc.Children.Add(layerDoc);
            }

            return doc;
        }

        public MultilayerNetwork Import(PortableDataDocument doc)
        {
            return CreateFromVectorDocumentV2(doc);
        }

        static MultilayerNetwork CreateFromVectorDocumentV2(PortableDataDocument doc)
        {
            var spec = NetworkSpecification.FromVectorDocument(doc.Children.First());
            var properties = doc.Properties.Where(p => p.Key.StartsWith("_")).ToDictionary(p => p.Key.Substring(1), p => p.Value);

            var network = new MultilayerNetwork(spec, properties);

            int i = 1;

            foreach (var layer in network.Layers)
            {
                layer.Import(doc.Children[i++]);
            }

            return network;
        }
    }
}