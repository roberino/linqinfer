using LinqInfer.Data;
using LinqInfer.Maths;
using System.Linq;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MultilayerNetworkExporter
    {
        /// <summary>
        /// Exports the raw data
        /// </summary>
        public BinaryVectorDocument Export(MultilayerNetwork network)
        {
            var doc = new BinaryVectorDocument();

            foreach (var prop in network.Properties)
            {
                doc.Properties["_" + prop.Key] = prop.Value;
            }

            doc.Properties["Activator"] = network.Parameters.Activator.Name;
            doc.Properties["ActivatorParameter"] = network.Parameters.Activator.Parameter.ToString();
            doc.Properties["InitialWeightRangeMin"] = network.Parameters.InitialWeightRange.Min.ToString();
            doc.Properties["InitialWeightRangeMax"] = network.Parameters.InitialWeightRange.Max.ToString();
            doc.Properties["LearningRate"] = network.Parameters.LearningRate.ToString();

            doc.Properties["Label"] = "Network";

            int i = 0;

            foreach (var layer in network.Layers)
            {
                i++;

                var layerDoc = layer.Export();

                layerDoc.Properties["Label"] = "Layer " + i;

                doc.Children.Add(layerDoc);
            }

            return doc;
        }

        public MultilayerNetwork Import(BinaryVectorDocument doc)
        {
            return CreateFromVectorDocument(doc);
        }

        private static MultilayerNetwork CreateFromVectorDocument(BinaryVectorDocument doc)
        {
            var activator = Activators.Create(doc.Properties["Activator"], double.Parse(doc.Properties["ActivatorParameter"]));
            var layerSizes = doc.Children.Select(c => int.Parse(c.Properties["Size"])).ToArray();

            var properties = doc.Properties.Where(p => p.Key.StartsWith("_")).ToDictionary(p => p.Key.Substring(1), p => p.Value);

            var network = new MultilayerNetwork(new NetworkParameters(layerSizes, activator)
            {
                LearningRate = double.Parse(doc.Properties["LearningRate"]),
                InitialWeightRange = new Range(double.Parse(doc.Properties["InitialWeightRangeMax"]), double.Parse(doc.Properties["InitialWeightRangeMin"]))
            }, properties);

            int i = 0;

            foreach (var layer in network.Layers)
            {
                layer.Import(doc.Children[i++]);
            }

            return network;
        }
    }
}