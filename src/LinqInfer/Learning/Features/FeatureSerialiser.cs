using System.Collections.Generic;
using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    static class FeatureSerialiser
    {
        const string FeaturePrefix = "_feature_";

        public static PortableDataDocument AppendFeatureAttributes(this PortableDataDocument doc, IReadOnlyCollection<IFeature> features, int? vectorSize = null)
        {
            foreach (var feature in features)
            {
                doc.Properties[FeaturePrefix + feature.Key] = feature.ToDictionary().ToDictionaryString();
            }

            doc.Properties["VectorSize"] = vectorSize.GetValueOrDefault(features.Count()).ToString();

            return doc;
        }

        public static (int vectorSize, IFeature[] features) LoadFeatureAttributes(this PortableDataDocument data)
        {
            var vectorSize = data.PropertyOrDefault("VectorSize", 0);

            if (vectorSize <= 0)
            {
                throw new InvalidDataException("Invalid vector size");
            }

            return (vectorSize, data
                .Properties
                .Where(p => p.Key.StartsWith(FeaturePrefix))
                .Select(x => x.Value.FromDictionaryString<string>())
                .Select(Feature.FromDictionary).ToArray());
        }
    }
}
