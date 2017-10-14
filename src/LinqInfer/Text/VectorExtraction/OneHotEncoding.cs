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
    internal class OneHotEncoding<T> : IFloatingPointFeatureExtractor<T>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly IDictionary<string, int> _vocabulary;
        private readonly Func<T, string> _objectToStringTransform;

        public OneHotEncoding(ISemanticSet vocabulary, Func<T, string> objectToStringTransform)
        {
            int i = 0;
            _objectToStringTransform = objectToStringTransform;
            _vocabulary = vocabulary.Words.ToDictionary(w => w.ToLowerInvariant(), _ => i++);
            FeatureMetadata = Feature.CreateDefaults(vocabulary.Words, DistributionModel.Categorical);
        }

        public int VectorSize => _vocabulary.Count;

        public bool IsNormalising => false;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return ExtractColumnVector(_objectToStringTransform(obj));
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            return NormaliseUsing(samples.Select(s => _objectToStringTransform(s)));
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractVector(_objectToStringTransform(obj));
        }

        public ColumnVector1D ExtractColumnVector(string token)
        {
            return new ColumnVector1D(ExtractVector(token));
        }

        public double[] ExtractVector(string token)
        {
            var vect = new double[_vocabulary.Count];

            if (_vocabulary.TryGetValue(token, out int index))
            {
                vect[index] = 1d;
            }

            return vect;
        }

        public ColumnVector1D Encode(IToken token)
        {
            return ExtractColumnVector(token.Text.ToLowerInvariant());
        }

        public double[] NormaliseUsing(IEnumerable<string> samples)
        {
            return Enumerable.Range(0, VectorSize).Select(n => 1d).ToArray();
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
            return new KeyValueDocument(_vocabulary.ToDictionary(k => k.Key, v => v.Value.ToString())).ToVectorDocument();
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            var kvdoc = new KeyValueDocument();

            kvdoc.FromVectorDocument(doc);

            foreach(var kv in kvdoc.Data)
            {
                _vocabulary[kv.Key] = int.Parse(kv.Value);
            }
        }
    }
}