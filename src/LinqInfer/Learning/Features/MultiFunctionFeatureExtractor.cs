using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Maths;
using LinqInfer.Data;

namespace LinqInfer.Learning.Features
{
    internal class MultiFunctionFeatureExtractor<T> : 
        IFloatingPointFeatureExtractor<T>, 
        IImportableAsVectorDocument, 
        IExportableAsVectorDocument where T : class
    {
        private static readonly ObjectFeatureExtractorFactory _objExtractor = new ObjectFeatureExtractorFactory();

        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;

        private FloatingPointFilteringFeatureExtractor<T> _filter;
        private FloatingPointTransformingFeatureExtractor<T> _transformation;

        private bool _normalisationCompleted;

        internal MultiFunctionFeatureExtractor(IFloatingPointFeatureExtractor<T> featureExtractor = null)
        {
            _featureExtractor = featureExtractor ?? _objExtractor.CreateFeatureExtractor<T>();
            _normalisationCompleted = false;
        }

        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        private IFloatingPointFeatureExtractor<T> FeatureExtractor
        {
            get
            {
                return _transformation ?? _filter ?? _featureExtractor;
            }
        }

        /// <summary>
        /// Returns the size of the vector returned
        /// when vectors are extracted
        /// </summary>
        public int VectorSize
        {
            get
            {
                return FeatureExtractor.VectorSize;
            }
        }

        /// <summary>
        /// Returns an enumeration of feature metadata
        /// </summary>
        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return FeatureExtractor.FeatureMetadata;
            }
        }

        public bool NormalisationCompleted
        {
            get
            {
                return _normalisationCompleted;
            }
        }

        public bool IsNormalising
        {
            get
            {
                return FeatureExtractor.IsNormalising;
            }
        }

        /// <summary>
        /// Filters features by property
        /// </summary>
        public void FilterFeaturesByProperty(Action<PropertySelector<T>> selector)
        {
            var ps = new PropertySelector<T>();

            selector(ps);

            if (ps.SelectedProperties.Any())
            {
                FilterFeatures(f => ps.SelectedProperties.Contains(f.Label));

                _normalisationCompleted = false;
            }
        }

        /// <summary>
        /// Filters features
        /// </summary>
        public void FilterFeatures(Func<IFeature, bool> featureFilter)
        {
            if (_transformation != null)
            {
                throw new InvalidOperationException("Features must be filtered before transformations and preprocessing - call this method first");
            }

            _filter = new FloatingPointFilteringFeatureExtractor<T>(_featureExtractor, featureFilter);

            _normalisationCompleted = false;
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        public void PreprocessWith(IVectorTransformation transformation)
        {
            if (_transformation == null)
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_filter ?? _featureExtractor, transformation);
            }
            else
            {
                _transformation.AddTransform(transformation);
            }
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return FeatureExtractor.ExtractColumnVector(obj);
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            return FeatureExtractor.NormaliseUsing(samples);
        }

        public double[] ExtractVector(T obj)
        {
            return FeatureExtractor.ExtractVector(obj);
        }

        /// <summary>
        /// Exports the state of the pipeline as a <see cref="BinaryVectorDocument"/>
        /// </summary>
        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["HasFilter"] = (_filter != null).ToString().ToLower();
            doc.Properties["HasTransformation"] = (_transformation != null).ToString().ToLower();

            doc.WriteChildObject(FeatureExtractor);

            return doc;
        }

        /// <summary>
        /// Retores the state of the pipeline as a <see cref="BinaryVectorDocument"/>
        /// </summary>
        public void FromVectorDocument(BinaryVectorDocument data)
        {
            var hasTr = data.PropertyOrDefault("HasTransformation", false);

            if (data.PropertyOrDefault("HasFilter", false))
            {
                _filter = new FloatingPointFilteringFeatureExtractor<T>(_featureExtractor, null);

                if (!hasTr)
                {
                    _filter.FromVectorDocument(data.Children.First());
                    return;
                }
            }

            if (data.PropertyOrDefault("HasTransformation", false))
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_filter ?? _featureExtractor);

                _transformation.FromVectorDocument(data.Children.First());

                return;
            }

            if (_featureExtractor is IImportableAsVectorDocument)
            {
                ((IImportableAsVectorDocument)_featureExtractor).FromVectorDocument(data.Children.First());
            }
            else
            {
                throw new NotSupportedException("Feature extractor does not support IImportableAsVectorDocument");
            }
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
    }
}
