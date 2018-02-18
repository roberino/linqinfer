using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class FloatingPointTransformingFeatureExtractor<TInput> : IFloatingPointFeatureExtractor<TInput>, IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly IFloatingPointFeatureExtractor<TInput> _baseFeatureExtractor;
        private readonly List<IVectorTransformation> _transformations;

        private IList<IFeature> _transformedFeatures;

        public FloatingPointTransformingFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor, IVectorTransformation transformation = null)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _transformations = new List<IVectorTransformation>();

            if (transformation != null) _transformations.Add(transformation);
        }

        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                if (_transformedFeatures == null)
                {
                    _transformedFeatures = Feature.CreateDefaults(VectorSize, TypeCode.Double, "Transform {0}");
                }
                return _transformedFeatures;
            }
        }

        public IDictionary<string, int> IndexLookup
        {
            get
            {
                return null;
            }
        }

        public int VectorSize { get { return _transformations.Any() ? _transformations.Last().OutputSize : _baseFeatureExtractor.VectorSize; } }

        public void AddTransform(IVectorTransformation transformation)
        {
            var last = _transformations.LastOrDefault();

            if (VectorSize == transformation.InputSize)
            {
                _transformations.Add(transformation);
                _transformedFeatures = null;
            }
            else
            {
                throw new ArgumentException($"Invalid input size - {transformation.InputSize}");
            }
        }

        public double[] ExtractVector(TInput obj)
        {
            return ExtractColumnVector(obj).GetUnderlyingArray();
        }

        public virtual void Load(Stream input)
        {
            _baseFeatureExtractor.Load(input);
        }

        public virtual void Save(Stream output)
        {
            _baseFeatureExtractor.Save(output);
        }

        private IVector Transform(IVector input)
        {
            IVector nextInput = input;

            foreach(var tx in _transformations)
            {
                nextInput = tx.Apply(nextInput);
            }

            return nextInput;
        }

        public ColumnVector1D ExtractColumnVector(TInput obj)
        {
            return ExtractIVector(obj).ToColumnVector();
        }

        public IVector ExtractIVector(TInput obj)
        {
            var nextInput = _baseFeatureExtractor.ExtractIVector(obj);

            foreach (var tx in _transformations)
            {
                nextInput = tx.Apply(nextInput);
            }

            return nextInput;
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            foreach (var tr in _transformations)
            {
                if (!(tr is IExportableAsVectorDocument))
                {
                    throw new NotSupportedException("Non-serialisable transformation");
                }

                doc.Children.Add(((IExportableAsVectorDocument)tr).ToVectorDocument());
            }

            doc.Properties["BaseFeatureExtractor"] = _baseFeatureExtractor.ToClob();

            return doc;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            _transformations.Clear();

            foreach(var child in doc.Children)
            {
                var tr = SerialisableVectorTransformation.LoadFromDocument(child);

                _transformations.Add(tr);
            }

            _baseFeatureExtractor.FromClob(doc.Properties["BaseFeatureExtractor"]);
        }
    }
}