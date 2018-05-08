using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    internal class TransformingFeatureExtractor<TInput, TVector> : IFeatureExtractor<TInput, TVector>, IExportableAsDataDocument, IImportableFromDataDocument where TVector : struct
    {
        private readonly IFeatureExtractor<TInput, TVector> _baseFeatureExtractor;
        private readonly Func<TVector[], TVector[]> _transformation;
        private readonly IList<IFeature> _selectedFeatures;
        private readonly IList<IFeature> _transformedFeatures;
        private readonly IDictionary<string, int> _indexLookup;

        private readonly int _vectorSize;
        private readonly bool _isFiltered;
        private readonly bool _hasCustomTransformation;

        public TransformingFeatureExtractor(IFeatureExtractor<TInput, TVector> baseFeatureExtractor, Func<TVector[], TVector[]> transformation, int[] indexSelection = null) : this(baseFeatureExtractor, transformation, indexSelection == null ? null : (Func<IFeature, bool>)(f => indexSelection.Contains(f.Index)))
        {
        }

        public TransformingFeatureExtractor(IFeatureExtractor<TInput, TVector> baseFeatureExtractor, Func<TVector[], TVector[]> transformation, Func<IFeature, bool> featureFilter)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _hasCustomTransformation = transformation != null;
            _transformation = transformation ?? (x => x);
            _isFiltered = featureFilter != null;
            _selectedFeatures = (!_isFiltered) ? baseFeatureExtractor.FeatureMetadata.ToList() : RebaseIndex(baseFeatureExtractor.FeatureMetadata.Where(featureFilter));
            _indexLookup = _selectedFeatures.ToDictionary(f => f.Key, f => f.Index);

            if (transformation == null)
            {
                _vectorSize = _baseFeatureExtractor.VectorSize;
            }
            else
            {
                _vectorSize = _transformation(new TVector[_selectedFeatures.Count]).Length;

                _transformedFeatures = Feature.CreateDefaults(_vectorSize, TypeCode.Double, "Transform {0}");
            }

            FeatureFilter = featureFilter;
        }

        internal Func<IFeature, bool> FeatureFilter { get; private set; }

        internal Func<TVector[], TVector[]> Transformation { get { return _transformation; } }
        
        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return _transformedFeatures ?? _selectedFeatures;
            }
        }

        public IDictionary<string, int> IndexLookup
        {
            get
            {
                return _indexLookup;
            }
        }

        public int InputSize { get { return _selectedFeatures.Count; } }

        public int VectorSize { get { return _vectorSize; } }

        public TVector[] ExtractVector(TInput obj)
        {
            var bnv = _baseFeatureExtractor.ExtractVector(obj);

            return FilterAndTransform(bnv);
        }

        public virtual void Load(Stream input)
        {
            var doc = new PortableDataDocument();
            doc.Load(input);
            ImportData(doc);
        }

        public virtual void Save(Stream output)
        {
            ExportData().Save(output);
        }

        public PortableDataDocument ExportData()
        {
            if (_hasCustomTransformation) throw new NotSupportedException("Custom transformations can't be serialised");

            var doc = new PortableDataDocument();

            doc.Properties["BaseFeatureExtractor"] = _baseFeatureExtractor.ToClob();

            if (_isFiltered) doc.Properties["SelectedFeatures"] = string.Join(",", _selectedFeatures.Select(f => f.Key));

            return doc;
        }

        public void ImportData(PortableDataDocument doc)
        {
            if (doc.Properties.ContainsKey("SelectedFeatures"))
            {
                var features = _baseFeatureExtractor.FeatureMetadata.ToDictionary(f => f.Key);
                var selectedFeatures = doc.Properties["SelectedFeatures"].Split(',').Select(k => features[k]);

                _selectedFeatures.Clear();

                foreach (var feature in selectedFeatures) _selectedFeatures.Add(feature);
            }

            if (doc.Properties.ContainsKey("BaseFeatureExtractor"))
            {
                _baseFeatureExtractor.FromClob(doc.Properties["BaseFeatureExtractor"]);
            }
        }

        private TVector[] FilterAndTransform(TVector[] vector)
        {
            return _transformation(Filter(vector));
        }

        private TVector[] Filter(TVector[] vector)
        {
            return _isFiltered ? _selectedFeatures.Select(f => vector[f.Index]).ToArray() : vector;
        }

        private static IList<IFeature> RebaseIndex(IEnumerable<IFeature> features)
        {
            var i = 0;
            return features
                .OrderBy(f => f.Index)
                .Select(f => (IFeature)new Feature()
                {
                    DataType = f.DataType,
                    Key = f.Key,
                    Label = f.Label,
                    Model = f.Model,
                    Index = i++
                })
                .ToList();
        }
    }
}