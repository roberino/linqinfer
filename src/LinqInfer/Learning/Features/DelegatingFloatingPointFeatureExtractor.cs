using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>, IFeatureExtractor<T, double>
    {
        private readonly int _vectorSize;
        private readonly bool _normaliseData;
        private readonly Func<T, float[]> _vectorFunc;
        private float[] _normalisingVector;

        public DelegatingFloatingPointFeatureExtractor(Func<T, float[]> vectorFunc, int vectorSize, bool normaliseData, string[] featureLabels)
            : this(vectorFunc, vectorSize, normaliseData, featureLabels == null ? null : Feature.CreateDefault(featureLabels))
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, float[]> vectorFunc, int vectorSize, bool normaliseData, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            _vectorSize = vectorSize;
            _normaliseData = normaliseData;
            
            FeatureMetadata = metadata ?? Feature.CreateDefault(_vectorSize);

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

        public float[] CreateNormalisingVector(T sample = default(T))
        {
            _normalisingVector = _vectorFunc(sample);

            return _normaliseData ? _normalisingVector.Select(v => 1f).ToArray() : _normalisingVector; // TODO: Fix this odd logic
        }

        public float[] NormaliseUsing(IEnumerable<T> samples)
        {
            _normalisingVector = Functions.MaxOfEachDimension(samples.Select(s => new ColumnVector1D(_vectorFunc(s)))).ToSingleArray();

            return _normalisingVector;
        }

        public float[] ExtractVector(T obj)
        {
            if (!_normaliseData) return _vectorFunc(obj);

            if (_normalisingVector == null) throw new InvalidOperationException("Normalising vector not defined");

            var raw = _vectorFunc(obj);
            var normalised = new float[raw.Length];

            for (int i = 0; i < raw.Length; i++)
            {
                normalised[i] = raw[i] / _normalisingVector[i];
            }

            return normalised;
        }

        double[] IFeatureExtractor<T, double>.CreateNormalisingVector(T sample)
        {
            return CreateNormalisingVector(sample).Select(x => (double)x).ToArray();
        }

        double[] IFeatureExtractor<T, double>.ExtractVector(T obj)
        {
            return ExtractVector(obj).Select(x => (double)x).ToArray();
        }

        double[] IFeatureExtractor<T, double>.NormaliseUsing(IEnumerable<T> samples)
        {
            return NormaliseUsing(samples).Select(x => (double)x).ToArray();
        }
    }
}