using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly Func<T, double[]> _vectorFunc;

        private readonly int _vectorSize;

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, string[] featureLabels)
            : this(vectorFunc, vectorSize, featureLabels == null ? null : Feature.CreateDefaults(featureLabels))
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            _vectorSize = vectorSize;
            
            FeatureMetadata = metadata ?? Feature.CreateDefaults(_vectorSize);

            IndexLookup = FeatureMetadata.ToDictionary(k => k.Label, v => v.Index);

            if (IndexLookup.Count != _vectorSize) throw new ArgumentException("Mismatch between labels count and vector size");
        }

        public int VectorSize
        {
            get
            {
                return _vectorSize;
            }
        }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public double[] ExtractVector(T obj)
        {
            return _vectorFunc(obj);
        }

        public void Save(Stream output)
        {
            ToVectorDocument().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new BinaryVectorDocument();

            doc.Load(input);

            FromVectorDocument(doc);
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return ExtractVector(obj);
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractColumnVector(obj);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["VectorSize"] = _vectorSize.ToString();

            return doc;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            if (doc.Vectors.Any())
            {
                var nv = doc.Vectors.First();

                if (nv.Size != _vectorSize)
                {
                    throw new ArgumentException("Invalid vector size");
                }
            }
        }
    }
}