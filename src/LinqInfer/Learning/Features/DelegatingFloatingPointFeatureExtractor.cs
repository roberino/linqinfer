using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>, IExportableAsDataDocument, IImportableFromDataDocument
    {
        private readonly Func<T, IVector> _vectorFunc;

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, string[] featureLabels)
            : this(vectorFunc, vectorSize, featureLabels == null ? null : Feature.CreateDefaults(featureLabels))
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, IFeature[] metadata = null) :
            this(x => new ColumnVector1D(vectorFunc(x)), vectorSize, metadata)
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, IVector> vectorFunc, int vectorSize, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            VectorSize = vectorSize;

            FeatureMetadata = metadata ?? Feature.CreateDefaults(VectorSize);

            IndexLookup = FeatureMetadata.ToDictionary(k => k.Label, v => v.Index);

            if (IndexLookup.Count != VectorSize) throw new ArgumentException("Mismatch between labels count and vector size");
        }

        public int VectorSize { get; }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public double[] ExtractVector(T obj)
        {
            return ExtractColumnVector(obj).GetUnderlyingArray();
        }

        public void Save(Stream output)
        {
            ExportData().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new PortableDataDocument();

            doc.Load(input);

            ImportData(doc);
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return _vectorFunc(obj).ToColumnVector();
        }

        public IVector ExtractIVector(T obj)
        {
            return _vectorFunc(obj);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.Properties[nameof(VectorSize)] = VectorSize.ToString();

            return doc;
        }

        public void ImportData(PortableDataDocument doc)
        {
            if (doc.Vectors.Any())
            {
                var nv = doc.Vectors.First();

                if (nv.Size != VectorSize)
                {
                    throw new ArgumentException("Invalid vector size");
                }
            }
        }
    }
}