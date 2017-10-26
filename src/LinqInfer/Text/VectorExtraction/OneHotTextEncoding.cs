using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;
using LinqInfer.Data;
using LinqInfer.Maths.Probability;

namespace LinqInfer.Text.VectorExtraction
{
    internal class OneHotTextEncoding<T> : IFloatingPointFeatureExtractor<T>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly OneHotEncoding<string> _encoder;
        private readonly Func<T, string> _objectToStringTransform;

        public OneHotTextEncoding(ISemanticSet vocabulary, Func<T, string> objectToStringTransform)
        {
            _objectToStringTransform = objectToStringTransform;
            _encoder = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, DistributionModel.Categorical);
        }

        public int VectorSize => _encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return ExtractColumnVector(_objectToStringTransform(obj));
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractVector(_objectToStringTransform(obj));
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractOneOfNVector(_objectToStringTransform(obj));
        }

        public ColumnVector1D ExtractColumnVector(string token)
        {
            return ExtractOneOfNVector(token).ToColumnVector();
        }

        public double[] ExtractVector(string token)
        {
            return ExtractOneOfNVector(token).ToColumnVector().GetUnderlyingArray();
        }

        public OneOfNVector ExtractOneOfNVector(string token)
        {
            return _encoder.Encode(token);
        }

        public ColumnVector1D Encode(IToken token)
        {
            return ExtractColumnVector(token.Text.ToLowerInvariant());
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