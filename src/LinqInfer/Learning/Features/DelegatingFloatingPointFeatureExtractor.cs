using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        private readonly int _vectorSize;
        private readonly bool _normaliseData;
        private readonly Func<T, float[]> _vectorFunc;
        private float[] _normalisingVector;

        public DelegatingFloatingPointFeatureExtractor(Func<T, float[]> vectorFunc, int vectorSize, bool normaliseData, string[] labels = null)
        {
            _vectorFunc = vectorFunc;
            _vectorSize = vectorSize;
            _normaliseData = normaliseData;

            int i = 0;

            Labels = (labels ?? Enumerable.Range(0, _vectorSize).Select(n => n.ToString()).ToArray()).ToDictionary(l => l, k => i++);
        }

        public int VectorSize
        {
            get
            {
                return _vectorSize;
            }
        }

        public IDictionary<string, int> Labels { get; private set; }

        public float[] CreateNormalisingVector(T sample = default(T))
        {
            _normalisingVector = _vectorFunc(sample);

            return _normaliseData ? _normalisingVector.Select(v => 1f).ToArray() : _normalisingVector;
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
    }
}
