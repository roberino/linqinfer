using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Text.VectorExtraction
{
    internal class OneHotTextEncoding<T> : IFloatingPointFeatureExtractor<T>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly OneHotEncoding<string> _encoder;
        private readonly Func<T, string[]> _objectToStringTransform;

        public OneHotTextEncoding(ISemanticSet vocabulary, Func<T, string> objectToStringTransform)
        {
            _objectToStringTransform = x => new[] { objectToStringTransform(x) };
            _encoder = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, FeatureVectorModel.Categorical);
        }

        public OneHotTextEncoding(ISemanticSet vocabulary, Func<T, string[]> objectToStringTransform)
        {
            _objectToStringTransform = objectToStringTransform;
            _encoder = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, FeatureVectorModel.Categorical);
        }

        public int VectorSize => _encoder.VectorSize;

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
            return _encoder.Encode(tokens);
        }

        public IVector Encode(IToken token)
        {
            return ExtractOneOfNVector(new[] { token.Text.ToLowerInvariant() });
        }

        public void Save(Stream output)
        {
            ToVectorDocument().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new BinaryVectorDocument(input);

            FromVectorDocument(doc);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            return new KeyValueDocument(_encoder.Lookup.ToDictionary(k => k.Key, v => v.Value.ToString())).ToVectorDocument();
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            var kvdoc = new KeyValueDocument();

            kvdoc.FromVectorDocument(doc);

            foreach (var kv in kvdoc.Data)
            {
                _encoder.Lookup[kv.Key] = int.Parse(kv.Value);
            }
        }
    }
}