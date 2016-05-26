using LinqInfer.Learning.Features;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    /// <summary>
    /// Represents a cluster of values.
    /// </summary>
    public class ClusterNode<T>
    {
        private readonly ConcurrentDictionary<T, int> _values;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly float _learningRate;

        public ClusterNode(IFloatingPointFeatureExtractor<T> featureExtractor, double[] initialWeights, float learningRate = 0.5f)
        {
            Contract.Assert(initialWeights.Length > 0);
            Contract.Assert(learningRate > 0);

            Weights = initialWeights;

            _featureExtractor = featureExtractor;
            _learningRate = learningRate;
            _values = new ConcurrentDictionary<T, int>();
        }

        public double[] Weights { get; private set; }

        internal bool IsInitialised { get; private set; }

        public IDictionary<T, int> GetMembers()
        {
            return _values.ToDictionary(w => w.Key, w => w.Value);
        }

        public bool IsMember(T value)
        {
            return MemberFrequency(value) > 0;
        }

        public int MemberFrequency(T value)
        {
            int f = 0;
            _values.TryGetValue(value, out f);
            return f;
        }

        public double CalculateDifference(T value)
        {
            return NetworkCalculator.CalculateDistance(_featureExtractor.ExtractVector(value), Weights);
        }

        internal double CalculateDifference(ObjectVector<T> dataItem)
        {
            if (IsMember(dataItem.Value)) return -1;
            return NetworkCalculator.CalculateDistance(dataItem.Attributes, Weights);
        }

        internal void AppendMember(ObjectVector<T> dataItem)
        {
            IsInitialised = true;
            bool isNew = false;

            if (_values.ContainsKey(dataItem.Value))
            {
                _values[dataItem.Value] += 1;
            }
            else
            {
                isNew = true;
                _values[dataItem.Value] = 1;
            }

            if (isNew)
            {
                lock(_values)
                {
                    Weights = NetworkCalculator.AdjustWeights(dataItem.Attributes, Weights, _learningRate);
                }
            }
        }
    }
}