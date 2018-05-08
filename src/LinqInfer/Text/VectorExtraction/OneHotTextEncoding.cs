using LinqInfer.Data;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text.VectorExtraction
{
    internal class OneHotTextEncoding<T> : IFloatingPointFeatureExtractor<T>, IExportableAsDataDocument, IImportableFromDataDocument
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
            ExportData().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new PortableDataDocument(input);

            ImportData(doc);
        }

        public PortableDataDocument ExportData()
        {
            return new KeyValueDocument(_encoder.Lookup.ToDictionary(k => k.Key, v => v.Value.ToString())).ExportData();
        }

        public void ImportData(PortableDataDocument doc)
        {
            var kvdoc = new KeyValueDocument();

            kvdoc.ImportData(doc);

            foreach (var kv in kvdoc.Data)
            {
                _encoder.Lookup[kv.Key] = int.Parse(kv.Value);
            }
        }
    }
}