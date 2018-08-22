using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class FeatureMapDataExtractor<T> : IFloatingPointFeatureExtractor<T>
        where T : class
    {
        readonly IFloatingPointFeatureExtractor<T> _baseFeatureExtractor;
        Matrix _weights;

        public FeatureMapDataExtractor(FeatureMap<T> map) : this(map.ExportClusterWeights(), map.Count(), map.FeatureExtractor)
        {
        }

        FeatureMapDataExtractor(Matrix weights, int vectorSize, IFloatingPointFeatureExtractor<T> baseFeatureExtractor)
        {
            _weights = weights;
            _baseFeatureExtractor = baseFeatureExtractor;

            VectorSize = vectorSize;
            FeatureMetadata = Feature.CreateDefaults(Enumerable.Range(1, VectorSize).Select(n => $"Cluster {n}"));
        }

        public int VectorSize { get; }

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            var vect = _baseFeatureExtractor.ExtractIVector(obj).ToColumnVector();

            return new ColumnVector1D(_weights.Select(v => vect.Distance(new ColumnVector1D(v))).ToArray());
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractColumnVector(obj).GetUnderlyingArray();
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractColumnVector(obj);
        }

        public static IFloatingPointFeatureExtractor<T> Create(PortableDataDocument doc,
            Func<PortableDataDocument, IFloatingPointFeatureExtractor<T>> baseFeatureExtractorLoader = null)
        {
            if (baseFeatureExtractorLoader == null)
            {
                baseFeatureExtractorLoader = FeatureExtractorFactory<T>.Default.Create;
            }

            var vectorSize = doc.PropertyOrDefault(nameof(VectorSize), 0);
            var weights = new Matrix(doc.Vectors.Select(v => v.ToColumnVector()));
            var bfe = baseFeatureExtractorLoader(doc.Children.First());

            return new FeatureMapDataExtractor<T>(weights, vectorSize, bfe);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetPropertyFromExpression(() => VectorSize);

            doc.Children.Add(_baseFeatureExtractor.ExportData());

            foreach (var row in _weights)
            {
                doc.Vectors.Add(new ColumnVector1D(row));
            }

            return doc;
        }
    }
}