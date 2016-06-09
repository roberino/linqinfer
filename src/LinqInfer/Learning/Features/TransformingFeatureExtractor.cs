using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class TransformingFeatureExtractor<TInput, TVector> : IFeatureExtractor<TInput, TVector> where TVector : struct
    {
        private readonly IFeatureExtractor<TInput, TVector> _baseFeatureExtractor;
        private readonly Func<TVector[], TVector[]> _transformation;
        private readonly int _vectorSize;
        private readonly IList<IFeature> _selectedFeatures;
        private readonly IList<IFeature> _transformedFeatures;
        private readonly IDictionary<string, int> _indexLookup;
        private readonly bool _isFiltered;

        public TransformingFeatureExtractor(IFeatureExtractor<TInput, TVector> baseFeatureExtractor, Func<TVector[], TVector[]> transformation, int[] indexSelection = null) : this(baseFeatureExtractor, transformation, indexSelection == null ? null : (Func<IFeature, bool>)(f => indexSelection.Contains(f.Index)))
        {
        }

        public TransformingFeatureExtractor(IFeatureExtractor<TInput, TVector> baseFeatureExtractor, Func<TVector[], TVector[]> transformation, Func<IFeature, bool> featureFilter)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
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

        public bool IsNormalising { get { return _baseFeatureExtractor.IsNormalising; } }

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

        public int VectorSize { get { return _vectorSize; } }

        public TVector[] CreateNormalisingVector(TInput sample = default(TInput))
        {
            var bnv = _baseFeatureExtractor.CreateNormalisingVector(sample);

            return FilterAndTransform(bnv);
        }

        public TVector[] NormaliseUsing(IEnumerable<TInput> samples)
        {
            var bnv = _baseFeatureExtractor.NormaliseUsing(samples);
            
            return FilterAndTransform(bnv);
        }

        public TVector[] ExtractVector(TInput obj)
        {
            var bnv = _baseFeatureExtractor.ExtractVector(obj);

            return FilterAndTransform(bnv);
        }

        public void Load(Stream input)
        {
            _baseFeatureExtractor.Load(input);
        }

        public void Save(Stream output)
        {
            _baseFeatureExtractor.Save(output);
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
