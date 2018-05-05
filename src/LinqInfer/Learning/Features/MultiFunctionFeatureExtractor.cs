using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Maths;
using LinqInfer.Data;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class MultiFunctionFeatureExtractor<T> : 
        IFloatingPointFeatureExtractor<T>, 
        IImportableFromDataDocument, 
        IExportableAsDataDocument where T : class
    {
        private static readonly ObjectFeatureExtractorFactory _objExtractor = new ObjectFeatureExtractorFactory();

        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;

        private FloatingPointFilteringFeatureExtractor<T> _filter;
        private FloatingPointTransformingFeatureExtractor<T> _transformation;

        internal MultiFunctionFeatureExtractor(IFloatingPointFeatureExtractor<T> featureExtractor = null)
        {
            _featureExtractor = featureExtractor ?? _objExtractor.CreateFeatureExtractor<T>();
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
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        public void PreprocessWith(ISerialisableDataTransformation transformation)
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
            return FeatureExtractor.ExtractIVector(obj).ToColumnVector();
        }

        public double[] ExtractVector(T obj)
        {
            return FeatureExtractor.ExtractVector(obj);
        }

        public IVector ExtractIVector(T obj)
        {
            return FeatureExtractor.ExtractIVector(obj);
        }

        /// <summary>
        /// Exports the state of the pipeline as a <see cref="PortableDataDocument"/>
        /// </summary>
        public PortableDataDocument ToDataDocument()
        {
            var doc = new PortableDataDocument();

            doc.Properties["HasFilter"] = (_filter != null).ToString().ToLower();
            doc.Properties["HasTransformation"] = (_transformation != null).ToString().ToLower();

            doc.WriteChildObject(FeatureExtractor);

            return doc;
        }

        /// <summary>
        /// Retores the state of the pipeline as a <see cref="PortableDataDocument"/>
        /// </summary>
        public void FromDataDocument(PortableDataDocument data)
        {
            var hasTr = data.PropertyOrDefault("HasTransformation", false);

            if (data.PropertyOrDefault("HasFilter", false))
            {
                _filter = new FloatingPointFilteringFeatureExtractor<T>(_featureExtractor, null);

                if (!hasTr)
                {
                    _filter.FromDataDocument(data.Children.First());
                    return;
                }
            }

            if (hasTr)
            {
                _transformation = new FloatingPointTransformingFeatureExtractor<T>(_filter ?? _featureExtractor);

                _transformation.FromDataDocument(data.Children.First());

                return;
            }

            if (_featureExtractor is IImportableFromDataDocument)
            {
                ((IImportableFromDataDocument)_featureExtractor).FromDataDocument(data.Children.First());
            }
            else
            {
                throw new NotSupportedException("Feature extractor does not support IImportableAsVectorDocument");
            }
        }

        public void Save(Stream output)
        {
            ToDataDocument().Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new PortableDataDocument();

            doc.Load(input);

            FromDataDocument(doc);
        }
    }
}
