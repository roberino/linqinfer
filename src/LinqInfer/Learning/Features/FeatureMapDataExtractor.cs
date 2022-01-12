using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class FeatureMapDataExtractor<TInput> : IVectorFeatureExtractor<TInput>
    {
        readonly IVectorFeatureExtractor<TInput> _baseFeatureExtractor;
        Matrix _weights;

        public FeatureMapDataExtractor(FeatureMap<TInput> map) : this(map.ExportClusterWeights(), map.Count(), map.FeatureExtractor)
        {
        }

        FeatureMapDataExtractor(Matrix weights, int vectorSize, IVectorFeatureExtractor<TInput> baseFeatureExtractor)
        {
            _weights = weights;
            _baseFeatureExtractor = baseFeatureExtractor;

            VectorSize = vectorSize;
            FeatureMetadata = Feature.CreateDefaults(Enumerable.Range(1, VectorSize).Select(n => $"Cluster {n}"));
        }

        public int VectorSize { get; }

        public IEnumerable<IFeature> FeatureMetadata { get; }

        public bool CanEncode(TInput obj) => true;

        public IVector ExtractIVector(TInput obj)
        {
            var vect = _baseFeatureExtractor.ExtractIVector(obj).ToColumnVector();

            return new ColumnVector1D(_weights.Select(v => vect.Distance(new ColumnVector1D(v))).ToArray());
        }

        public static IVectorFeatureExtractor<TInput> Create(PortableDataDocument doc,
            Func<PortableDataDocument, IVectorFeatureExtractor<TInput>> baseFeatureExtractorLoader = null)
        {
            if (baseFeatureExtractorLoader == null)
            {
                baseFeatureExtractorLoader = FeatureExtractorFactory<TInput>.Default.Create;
            }

            var vectorSize = doc.PropertyOrDefault(nameof(VectorSize), 0);
            var weights = new Matrix(doc.Vectors.Select(v => v.ToColumnVector()));
            var bfe = baseFeatureExtractorLoader(doc.Children.First());

            return new FeatureMapDataExtractor<TInput>(weights, vectorSize, bfe);
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