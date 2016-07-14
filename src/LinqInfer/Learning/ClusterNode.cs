﻿using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    /// <summary>
    /// Represents a cluster of values.
    /// </summary>
    public class ClusterNode<T> : IGrouping<double, T>
    {
        private readonly ConcurrentDictionary<T, int> _values;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly float _learningRate;

        internal ClusterNode(IFloatingPointFeatureExtractor<T> featureExtractor, double[] initialWeights, float learningRate = 0.5f)
        {
            Contract.Assert(initialWeights.Length > 0);
            Contract.Assert(learningRate > 0);

            Weights = new ColumnVector1D(initialWeights);

            _featureExtractor = featureExtractor;
            _learningRate = learningRate;
            _values = new ConcurrentDictionary<T, int>();
        }

        public ColumnVector1D Weights { get; private set; }

        internal bool IsInitialised { get; private set; }

        public double Key
        {
            get
            {
                return Weights.EuclideanLength;
            }
        }

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
            return _featureExtractor.ExtractColumnVector(value).Distance(Weights);
        }

        internal double CalculateDifference(ObjectVector<T> dataItem)
        {
            if (IsMember(dataItem.Value)) return -1;

            return dataItem.Vector.Distance(Weights);
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
                lock (_values)
                {
                    Weights.Apply((w, i) => w + _learningRate * (dataItem.Vector[i] - w));
                }
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _values.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}