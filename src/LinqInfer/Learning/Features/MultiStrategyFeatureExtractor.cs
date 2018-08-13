using System;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class MultiStrategyFeatureExtractor<T> : 
        IFloatingPointFeatureExtractor<T>,
        IImportableFromDataDocument
    {
        readonly IFloatingPointFeatureExtractor<T>[] _featureExtractionStrategies;

        public MultiStrategyFeatureExtractor(params IFloatingPointFeatureExtractor<T>[] featureExtractionStrategies)
        {
            _featureExtractionStrategies = featureExtractionStrategies;
        }

        public int VectorSize => _featureExtractionStrategies.Sum(s => s.VectorSize);

        public IEnumerable<IFeature> FeatureMetadata => _featureExtractionStrategies.SelectMany(s => s.FeatureMetadata);

        public IVector ExtractIVector(T obj)
        {
            var vects = _featureExtractionStrategies.Select(f => f.ExtractIVector(obj));

            return new MultiVector(vects.ToArray());
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractIVector(obj).ToColumnVector().GetUnderlyingArray();
        }

        public void ImportData(PortableDataDocument doc)
        {
            for (var i = 0; i < _featureExtractionStrategies.Length; i++)
            {
                doc.ReadChildObject(_featureExtractionStrategies[i], i);
            }
        }

        public static IFloatingPointFeatureExtractor<T> Create(PortableDataDocument doc,
            Func<PortableDataDocument, IFloatingPointFeatureExtractor<T>> baseFeatureExtractorLoader)
        {
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