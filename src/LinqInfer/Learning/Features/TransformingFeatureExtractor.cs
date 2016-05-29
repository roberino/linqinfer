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
        private readonly IDictionary<string, int> _indexLookup;

        public TransformingFeatureExtractor(IFeatureExtractor<TInput, TVector> baseFeatureExtractor, Func<TVector[], TVector[]> transformation, int[] indexSelection = null)
        {
            _baseFeatureExtractor = baseFeatureExtractor;
            _transformation = transformation;
            _selectedFeatures = (indexSelection == null) ? baseFeatureExtractor.FeatureMetadata.ToList() : baseFeatureExtractor.FeatureMetadata.Where(f => indexSelection.Contains(f.Index)).ToList();
            _indexLookup = _selectedFeatures.ToDictionary(f => f.Key, f => f.Index);
            _vectorSize = transformation(new TVector[_baseFeatureExtractor.VectorSize]).Length;
        }

        public IEnumerable<IFeature> FeatureMetadata
        {
            get
            {
                return _selectedFeatures;
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
            var bnv = _baseFeatureExtractor.CreateNormalisingVector(obj);

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
            return _selectedFeatures.Select(f => vector[f.Index]).ToArray();
        }
    }
}
