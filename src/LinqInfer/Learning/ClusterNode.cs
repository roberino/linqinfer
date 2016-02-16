using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    public class ClusterNode<T>
    {
        private readonly ConcurrentDictionary<T, int> _words;
        private readonly float _learningRate;

        public ClusterNode(float[] initialWeights, float learningRate = 0.5f)
        {
            Contract.Assert(initialWeights.Length > 0);
            Contract.Assert(learningRate > 0);

            Weights = initialWeights;

            _learningRate = learningRate;
            _words = new ConcurrentDictionary<T, int>();
        }

        public float[] Weights { get; private set; }

        public bool IsInitialised { get; private set; }

        public IDictionary<T, int> Data
        {
            get
            {
                return _words.ToDictionary(w => w.Key, w => w.Value);
            }
        }

        public float CalculateDifference(ObjectVector<T> dataItem)
        {
            if (_words.ContainsKey(dataItem.Value)) return -1;
            return NetworkCalculator.CalculateDistance(dataItem.Attributes, Weights);
        }

        public void AdjustAndAppend(ObjectVector<T> dataItem)
        {
            IsInitialised = true;
            bool isNew = false;

            if (_words.ContainsKey(dataItem.Value))
            {
                _words[dataItem.Value] += 1;
            }
            else
            {
                isNew = true;
                _words[dataItem.Value] = 1;
            }

            if (isNew)
            {
                lock(_words)
                {
                    Weights = NetworkCalculator.AdjustWeights(dataItem.Attributes, Weights, _learningRate);
                }
            }
        }
    }
}