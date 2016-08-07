using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
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
        private readonly float _initialLearningRate;
        private readonly Func<float, int, int, double> _learningRateDecayFunction;

        /// <summary>
        /// Creates a new node.
        /// </summary>
        /// <param name="featureExtractor">A feature extractor to extract vector data</param>
        /// <param name="initialWeights">The initial set of weights</param>
        /// <param name="initialLearningRate">The initial learning rate</param>
        /// <param name="initialRadius">When set, this is used to determine the radius of the cluster node which is used to calculate the influence this node has on neighbouring nodes when updating weights.</param>
        /// <param name="learningRateDecayFunction">A function which take the initial rate, current iteration and number of iteration</param>
        internal ClusterNode(IFloatingPointFeatureExtractor<T> featureExtractor, double[] initialWeights, float initialLearningRate = 0.5f, float? initialRadius = null, Func<float, int, int, double> learningRateDecayFunction = null)
        {
            Contract.Assert(initialWeights.Length > 0);
            Contract.Assert(initialLearningRate > 0);

            Weights = new ColumnVector1D(initialWeights);
            InitialRadius = initialRadius;

            _featureExtractor = featureExtractor;
            _initialLearningRate = initialLearningRate;
            _learningRateDecayFunction = initialRadius.HasValue ? (learningRateDecayFunction ?? ((r, i, t) => r * Math.Exp(-((double)i / t)))) : (learningRateDecayFunction ?? ((r, i, t) => r));
            _values = new ConcurrentDictionary<T, int>();
        }

        /// <summary>
        /// Returns the weights associated with the cluster.
        /// </summary>
        public ColumnVector1D Weights { get; private set; }

        /// <summary>
        /// Return true if the node has been initialised.
        /// </summary>
        internal bool IsInitialised { get; private set; }

        /// <summary>
        /// When set, this is used to determine the radius of the cluster node 
        /// which is used to calculate the influence this node has on neighbouring nodes
        /// when updating weights.
        /// </summary>
        internal double? InitialRadius { get; private set; }

        internal double? CurrentRadius { get; private set; }

        /// <summary>
        /// The Euclidean distance from the centre is represented as the key.
        /// </summary>
        public double Key
        {
            get
            {
                return Weights.Distance(Enumerable.Range(0, Weights.Size).Select(n => 0.5d).ToArray());
            }
        }

        /// <summary>
        /// Returns a dictionary of members and frequencies within the cluster.
        /// </summary>
        /// <returns></returns>
        public IDictionary<T, int> GetMembers()
        {
            return _values.ToDictionary(w => w.Key, w => w.Value);
        }

        /// <summary>
        /// Returns true if a member belongs to the cluster.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsMember(T value)
        {
            return MemberFrequency(value) > 0;
        }

        /// <summary>
        /// Returns the frequency of an item.
        /// </summary>
        public int MemberFrequency(T value)
        {
            int f = 0;
            _values.TryGetValue(value, out f);
            return f;
        }

        /// <summary>
        /// Calculates the Euclidean distance of the node from a value.
        /// </summary>
        public double CalculateDifference(T value)
        {
            return _featureExtractor.ExtractColumnVector(value).Distance(Weights);
        }

        internal double CalculateDifference(ObjectVector<T> dataItem)
        {
            if (IsMember(dataItem.Value)) return -1;

            return dataItem.Vector.Distance(Weights);
        }

        internal void AppendMember(ObjectVector<T> dataItem, bool adjust = true)
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

            if (isNew && adjust)
            {
                AdjustFor(dataItem.Vector);
            }
        }

        internal void AdjustForIteration(IEnumerable<ClusterNode<T>> otherNodes, ObjectVector<T> dataItem, int iteration, int numberOfIterations)
        {
            AppendMember(dataItem, !InitialRadius.HasValue);

            if (InitialRadius.HasValue)
            {
                var neighbourhoodRadius = CurrentNeighbourhoodRadius(iteration, numberOfIterations);

                CurrentRadius = neighbourhoodRadius;

                var otherNodesAndDist = otherNodes.Select(o => new
                {
                    distance = o.Weights.Distance(Weights),
                    node = o
                })
                .Where(o => o.distance < neighbourhoodRadius);

                foreach (var neighbourhoodNode in otherNodesAndDist)
                {
                    var influence = Math.Exp(-((neighbourhoodNode.distance) / (2 * neighbourhoodRadius)));

                    neighbourhoodNode.node.AdjustFor(Weights, iteration, numberOfIterations, influence);
                }
            }
        }

        private void AdjustFor(ColumnVector1D vector, int iteration = 0, int numberOfIterations = 1, double influence = 1)
        {
            lock (_values)
            {
                var l = _learningRateDecayFunction(_initialLearningRate, iteration, numberOfIterations);
                var r = l * influence;
                Weights.Apply((w, i) => w + r * (vector[i] - w));
            }
        }
        
        private double CurrentNeighbourhoodRadius(int iteration, int numberOfIterations)
        {
            var r = InitialRadius.Value;
            var t = numberOfIterations / Math.Log(r);
            return r * Math.Exp((-iteration) / t);
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