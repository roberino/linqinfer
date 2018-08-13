using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Learning.Features
{
    public class CategoricalFeatureExtractor<TInput, TCategory> : IFloatingPointFeatureExtractor<TInput>
    {
        readonly OneHotEncoding<TCategory> _encoder;
        readonly Func<TInput, TCategory> _categorySelector;

        internal CategoricalFeatureExtractor(Func<TInput, TCategory> categorySelector, IEnumerable<IFeature> features, ISet<TCategory> categories = null)
        {
            _encoder = new OneHotEncoding<TCategory>(categories ?? new HashSet<TCategory>());
            _categorySelector = categorySelector;
            FeatureMetadata = features;
        }

        public int VectorSize => _encoder.VectorSize;

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public IVector ExtractIVector(TInput obj)
        {
            return _encoder.Encode(_categorySelector(obj));
        }

        public double[] ExtractVector(TInput obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        void Load(Stream stream)
        {
            var sz = new DictionarySerialiser<TCategory, int>();

            var lookup = sz.Read(stream);

            foreach(var item in lookup)
            {
                _encoder.Lookup[item.Key] = item.Value;
            }
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            using (var ms = new MemoryStream())
            {
                var sz = new DictionarySerialiser<TCategory, int>();

                sz.Write(_encoder.Lookup, ms);

                doc.Blobs.Add(nameof(_encoder.Lookup), ms.ToArray());
            }

            return doc;
        }
    }
}