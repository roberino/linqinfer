using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class BaseFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        const string FeaturePrefix = "_feature_";

        readonly Func<T, IVector> _vectorFunc;
        readonly bool _customFeatureLabels;

        public BaseFeatureExtractor(Func<T, IVector> vectorFunc, int vectorSize, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            _customFeatureLabels = metadata != null;

            VectorSize = vectorSize;

            FeatureMetadata = metadata ?? Feature.CreateDefaults(VectorSize);
        }

        public BaseFeatureExtractor(BaseFeatureExtractor<T> other)
        {
            _vectorFunc = other._vectorFunc;
            _customFeatureLabels = other._customFeatureLabels;

            VectorSize = other.VectorSize;

            FeatureMetadata = other.FeatureMetadata;
        }

        public int VectorSize { get; }

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public IVector ExtractIVector(T obj)
        {
            return _vectorFunc(obj);
        }

        public virtual PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);

            if (_customFeatureLabels)
            {
                foreach (var feature in FeatureMetadata)
                {
                    doc.Properties[FeaturePrefix + feature.Key] = feature.ToDictionary().ToDictionaryString();
                }
            }

            doc.SetPropertyFromExpression(() => VectorSize);

            return doc;
        }

        protected static (int vectorSize, IFeature[] features) LoadAttributes(PortableDataDocument data)
        {
            var vectorSize = data.PropertyOrDefault(nameof(VectorSize), 0);

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