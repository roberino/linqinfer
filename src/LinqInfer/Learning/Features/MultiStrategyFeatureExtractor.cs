using System;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class MultiStrategyFeatureExtractor<T> : IVectorFeatureExtractor<T>
    {
        readonly IVectorFeatureExtractor<T>[] _featureExtractionStrategies;

        public MultiStrategyFeatureExtractor(params IVectorFeatureExtractor<T>[] featureExtractionStrategies)
        {
            _featureExtractionStrategies = featureExtractionStrategies;
        }

        public int VectorSize => _featureExtractionStrategies.Sum(s => s.VectorSize);

        public IEnumerable<IFeature> FeatureMetadata => _featureExtractionStrategies.SelectMany(s => s.FeatureMetadata);

        public bool CanEncode(T obj) => _featureExtractionStrategies.Any(f => f.CanEncode(obj));

        public IVector ExtractIVector(T obj)
        {
            var vects = _featureExtractionStrategies.Select(f => f.ExtractIVector(obj));

            return new MultiVector(vects.ToArray());
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public static IVectorFeatureExtractor<T> Create(PortableDataDocument doc,
            Func<PortableDataDocument, IVectorFeatureExtractor<T>> baseFeatureExtractorLoader = null)
        {
            if (baseFeatureExtractorLoader == null)
            {
                baseFeatureExtractorLoader = FeatureExtractorFactory<T>.Default.Create;
            }

            var featureExtractionStrategies = doc.Children
                .Select(baseFeatureExtractorLoader)
                .ToArray();

            return new MultiStrategyFeatureExtractor<T>(featureExtractionStrategies);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetType(this);

            foreach (var fe in _featureExtractionStrategies)
            {
                doc.WriteChildObject(fe);
            }

            return doc;
        }
    }
}