using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace LinqInfer.Learning.Features
{
    [Serializable]
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        [NonSerialized]
        private readonly Func<T, double[]> _vectorFunc;

        private readonly int _vectorSize;
        private readonly bool _normaliseData;
        private double[] _normalisingVector;

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, bool normaliseData, string[] featureLabels)
            : this(vectorFunc, vectorSize, normaliseData, featureLabels == null ? null : Feature.CreateDefault(featureLabels))
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, bool normaliseData, IFeature[] metadata = null)
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

        public double[] CreateNormalisingVector(T sample = default(T))
        {
            _normalisingVector = _vectorFunc(sample);

            return _normaliseData ? _normalisingVector.Select(v => 1d).ToArray() : _normalisingVector; // TODO: Fix this odd logic
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            _normalisingVector = Functions.MaxOfEachDimension(samples.Select(s => new ColumnVector1D(_vectorFunc(s)))).ToDoubleArray();

            return _normalisingVector;
        }

        public double[] ExtractVector(T obj)
        {
            if (!_normaliseData) return _vectorFunc(obj);

            if (_normalisingVector == null) throw new InvalidOperationException("Normalising vector not defined");

            var raw = _vectorFunc(obj);
            var normalised = new double[raw.Length];

            for (int i = 0; i < raw.Length; i++)
            {
                normalised[i] = raw[i] / _normalisingVector[i];
            }

            return normalised;
        }

        public void Save(Stream output)
        {
            var sz = new BinaryFormatter();

            sz.Serialize(output, _normalisingVector);
        }

        public void Load(Stream input)
        {
            var sz = new BinaryFormatter();

            var normVect = sz.Deserialize(input) as double[];

            _normalisingVector = normVect;
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return new ColumnVector1D(ExtractVector(obj));
        }
    }
}