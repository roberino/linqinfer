using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace LinqInfer.Learning.Features
{
    internal class DelegatingFloatingPointFeatureExtractor<T> : IFloatingPointFeatureExtractor<T>
    {
        private readonly Func<T, double[]> _vectorFunc;

        private readonly int _vectorSize;
        private readonly bool _normaliseData;
        private double[] _normalisingVector;

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, bool normaliseData, string[] featureLabels)
            : this(vectorFunc, vectorSize, normaliseData, featureLabels == null ? null : Feature.CreateDefaults(featureLabels))
        {
        }

        public DelegatingFloatingPointFeatureExtractor(Func<T, double[]> vectorFunc, int vectorSize, bool normaliseData, IFeature[] metadata = null)
        {
            _vectorFunc = vectorFunc;
            _vectorSize = vectorSize;
            _normaliseData = normaliseData;
            
            FeatureMetadata = metadata ?? Feature.CreateDefaults(_vectorSize);

            IndexLookup = FeatureMetadata.ToDictionary(k => k.Label, v => v.Index);

            if (IndexLookup.Count != _vectorSize) throw new ArgumentException("Mismatch between labels count and vector size");
        }

        public bool IsNormalising { get { return _normaliseData; } }

        public int VectorSize
        {
            get
            {
                return _vectorSize;
            }
        }

        public IDictionary<string, int> IndexLookup { get; private set; }

        public IEnumerable<IFeature> FeatureMetadata { get; private set; }

        public double[] UpperVectorBounds
        {
            get
            {
                return _normaliseData ? _normalisingVector.Select(v => 1d).ToArray() : _normalisingVector;
            }
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            if (samples.Any())
            {
                _normalisingVector = Functions.MaxOfEachDimension(samples.Select(s => new ColumnVector1D(_vectorFunc(s)))).ToDoubleArray();
            }
            else
            {
                if (_normalisingVector == null)
                    _normalisingVector = _vectorFunc(default(T)); // TODO: remove this odd logic
            }

            //for (int i = 0; i < _normalisingVector.Length; i++)
            //{
            //    if(_normalisingVector[i] == 0)
            //    {
            //        _normalisingVector[i] = 0.0000001;
            //    }
            //}

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
                var x = raw[i] / _normalisingVector[i];
                normalised[i] = double.IsNaN(x) ? 0 : x;
            }

            return normalised;
        }

        public void Save(Stream output)
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["VectorSize"] = _vectorSize.ToString();
            doc.Properties["NormaliseData"] = _normaliseData.ToString();

            if (_normalisingVector != null)
                doc.Vectors.Add(new ColumnVector1D(_normalisingVector));

            doc.Save(output);
        }

        public void Load(Stream input)
        {
            var doc = new BinaryVectorDocument();

            doc.Load(input);

            if (doc.Vectors.Any())
            {
                var nv = doc.Vectors.First();

                if (nv.Size != _vectorSize)
                {
                    throw new ArgumentException("Invalid vector size");
                }

                _normalisingVector = nv.ToDoubleArray();
            }
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return new ColumnVector1D(ExtractVector(obj));
        }
    }
}