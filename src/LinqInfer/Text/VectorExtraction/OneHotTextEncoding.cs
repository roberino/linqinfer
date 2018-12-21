using LinqInfer.Data;
using LinqInfer.Data.Serialisation;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.VectorExtraction
{
    class OneHotTextEncoding<T> : IFloatingPointFeatureExtractor<T>, IImportableFromDataDocument
    {
        readonly Func<T, string[]> _objectToStringTransform;

        public OneHotTextEncoding(ISemanticSet vocabulary, Func<T, string> objectToStringTransform)
        {
            _objectToStringTransform = x => new[] { objectToStringTransform(x) };
            Encoder = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, FeatureVectorModel.Categorical);
        }

        public OneHotTextEncoding(ISemanticSet vocabulary, Func<T, string[]> objectToStringTransform)
        {
            _objectToStringTransform = objectToStringTransform;
            Encoder = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, FeatureVectorModel.Categorical);
        }

        public OneHotEncoding<string> Encoder { get; }

        public int VectorSize => Encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public double[] ExtractVector(T obj)
        {
            return ExtractVector(_objectToStringTransform(obj));
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractOneOfNVector(_objectToStringTransform(obj));
        }

        public double[] ExtractVector(string[] tokens)
        {
            return ExtractOneOfNVector(tokens).ToColumnVector().GetUnderlyingArray();
        }

        public IVector ExtractOneOfNVector(string[] tokens)
        {
            return Encoder.Encode(tokens);
        }

        public IVector Encode(IToken token)
        {
            return ExtractOneOfNVector(new[] { token.Text.ToLowerInvariant() });
        }

        public PortableDataDocument ExportData()
        {
            return new KeyValueDocument(Encoder.Lookup.ToDictionary(k => k.Key, v => v.Value.ToString())).ExportData();
        }

        public void ImportData(PortableDataDocument doc)
        {
            var kvdoc = new KeyValueDocument();

            kvdoc.ImportData(doc);

            foreach (var kv in kvdoc.Data)
            {
                Encoder.Lookup[kv.Key] = int.Parse(kv.Value);
            }
        }
    }
}