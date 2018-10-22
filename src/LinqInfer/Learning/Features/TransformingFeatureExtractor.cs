using LinqInfer.Data.Serialisation;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class TransformingFeatureExtractor<TInput> : IFloatingPointFeatureExtractor<TInput>
    {
        readonly IFloatingPointFeatureExtractor<TInput> _baseFeatureExtractor;
        readonly List<ISerialisableDataTransformation> _transformations;

        IList<IFeature> _transformedFeatures;

        public TransformingFeatureExtractor(IFloatingPointFeatureExtractor<TInput> baseFeatureExtractor)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _transformations = new List<ISerialisableDataTransformation>();
        }

        public IEnumerable<IFeature> FeatureMetadata => _transformedFeatures ??
                                                        (_transformedFeatures =
                                                            Feature.CreateDefaults(VectorSize, TypeCode.Double,
                                                                "Transform {0}"));

        public int VectorSize => _transformations.Any() ? _transformations.Last().OutputSize : _baseFeatureExtractor.VectorSize;

        public void AddTransform(ISerialisableDataTransformation transformation)
        {
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

        public PortableDataDocument ExportData()
        {
            var tranformations = new PortableDataDocument();

            foreach (var tr in _transformations)
            {
                tranformations.Children.Add(tr.ExportData());
            }

            var doc = new PortableDataDocument();

            doc.SetType(this);

            doc.Children.Add(tranformations);
            doc.Children.Add(_baseFeatureExtractor.ExportData());

            return doc;
        }

        public static TransformingFeatureExtractor<TInput> Create(PortableDataDocument doc, Func<PortableDataDocument, IFloatingPointFeatureExtractor<TInput>> baseFeatureExtractorLoader = null)
        {
            if (doc.Children.Count != 2)
            {
                throw new InvalidDataException("Expecting 2 child docs");
            }

            var baseExtractorDoc = doc.Children[1];

            if (baseFeatureExtractorLoader == null)
            {
                baseFeatureExtractorLoader = FeatureExtractorFactory<TInput>.Default.Create;
            }

            var bfe = baseFeatureExtractorLoader(baseExtractorDoc);

            var fe = new TransformingFeatureExtractor<TInput>(bfe);
            
            var transformationDoc = doc.Children[0];
            var dft = new DataTransformationFactory();

            foreach(var child in transformationDoc.Children)
            {
                fe.AddTransform(dft.Create(child));
            }

            return fe;
        }
    }
}