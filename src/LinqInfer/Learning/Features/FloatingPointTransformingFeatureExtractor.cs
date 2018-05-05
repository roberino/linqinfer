using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class FloatingPointTransformingFeatureExtractor<TInput> : IFloatingPointFeatureExtractor<TInput>, IExportableAsDataDocument, IImportableFromDataDocument
    {
        private readonly IFloatingPointFeatureExtractor<TInput> _baseFeatureExtractor;
        private readonly List<ISerialisableDataTransformation> _transformations;

        private IList<IFeature> _transformedFeatures;

        public FloatingPointTransformingFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor, ISerialisableDataTransformation transformation = null)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _transformations = new List<ISerialisableDataTransformation>();

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

        public void AddTransform(ISerialisableDataTransformation transformation)
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

        public PortableDataDocument ToDataDocument()
        {
            var doc = new PortableDataDocument();

            foreach (var tr in _transformations)
            {
                if (!(tr is IExportableAsDataDocument))
                {
                    throw new NotSupportedException("Non-serialisable transformation");
                }

                doc.Children.Add(((IExportableAsDataDocument)tr).ToDataDocument());
            }

            doc.Properties["BaseFeatureExtractor"] = _baseFeatureExtractor.ToClob();

            return doc;
        }

        public void FromDataDocument(PortableDataDocument doc)
        {
            _transformations.Clear();

            foreach(var child in doc.Children)
            {
                var tr = SerialisableDataTransformation.LoadFromDocument(child);

                _transformations.Add(tr);
            }

            _baseFeatureExtractor.FromClob(doc.Properties["BaseFeatureExtractor"]);
        }
    }
}