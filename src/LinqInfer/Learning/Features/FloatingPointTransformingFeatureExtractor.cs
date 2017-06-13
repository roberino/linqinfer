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
        private readonly IFeatureExtractor<TInput, double> _baseFeatureExtractor;
        private readonly List<IVectorTransformation> _transformations;

        private IList<IFeature> _transformedFeatures;

        public FloatingPointTransformingFeatureExtractor(IFeatureExtractor<TInput, double> baseFeatureExtractor, IVectorTransformation transformation = null)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _transformations = new List<IVectorTransformation>();

            if (transformation != null) _transformations.Add(transformation);
        }

        public bool IsNormalising { get { return _baseFeatureExtractor.IsNormalising; } }

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

        public double[] NormaliseUsing(IEnumerable<TInput> samples)
        {
            var bnv = _baseFeatureExtractor.NormaliseUsing(samples);
            
            return Transform(bnv);
        }

        public double[] ExtractVector(TInput obj)
        {
            var bnv = _baseFeatureExtractor.ExtractVector(obj);

            return Transform(bnv);
        }

        public virtual void Load(Stream input)
        {
            _baseFeatureExtractor.Load(input);
        }

        public virtual void Save(Stream output)
        {
            _baseFeatureExtractor.Save(output);
        }

        private double[] Transform(double[] input)
        {
            var nextInput = new ColumnVector1D(input);

            foreach(var tx in _transformations)
            {
                nextInput = new ColumnVector1D(tx.Apply(nextInput));
            }

            return nextInput.GetUnderlyingArray();
        }

        public ColumnVector1D ExtractColumnVector(TInput obj)
        {
            var bnv = _baseFeatureExtractor.ExtractVector(obj);
            var nextInput = new ColumnVector1D(bnv);

            foreach (var tx in _transformations)
            {
                nextInput = new ColumnVector1D(tx.Apply(nextInput));
            }

            return new ColumnVector1D(nextInput);
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
                var tr = new SerialisableVectorTransformation(Matrix.IdentityMatrix(1));

                tr.FromVectorDocument(child);

                _transformations.Add(tr);
            }

            _baseFeatureExtractor.FromClob(doc.Properties["BaseFeatureExtractor"]);
        }
    }
}