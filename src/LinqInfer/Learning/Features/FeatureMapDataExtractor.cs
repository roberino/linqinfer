using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class FeatureMapDataExtractor<T> : IFloatingPointFeatureExtractor<T>, IExportableAsDataDocument, IImportableFromDataDocument
    {
        private readonly IFloatingPointFeatureExtractor<T> _baseFeatureExtractor;
        private Matrix _weights;

        public FeatureMapDataExtractor(FeatureMap<T> map)
        {
            _baseFeatureExtractor = map.FeatureExtractor;
            _weights = map.ExportClusterWeights();

            VectorSize = map.Count();
            FeatureMetadata = Feature.CreateDefaults(Enumerable.Range(1, VectorSize).Select(n => $"Cluster {n}"));
        }

        public int VectorSize { get; private set; }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

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

        public void ImportData(PortableDataDocument doc)
        {
            _weights = new Matrix(doc.Vectors.Select(v => v.ToColumnVector()));
            _baseFeatureExtractor.FromClob(doc.Properties["BaseFeatureExtractor"]);
        }

        public void Load(Stream input)
        {
            var doc = new PortableDataDocument(input);

            ImportData(doc);
        }

        public void Save(Stream output)
        {
            ExportData().Save(output);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.Properties["BaseFeatureExtractor"] = _baseFeatureExtractor.ToClob();

            foreach (var row in _weights)
            {
                doc.Vectors.Add(new ColumnVector1D(row));
            }

            return doc;
        }
    }
}