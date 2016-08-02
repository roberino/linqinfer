using LinqInfer.Learning.Features;
using System.Collections;
using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public class FeatureMap<T> : IEnumerable<ClusterNode<T>>
    {
        private readonly IEnumerable<ClusterNode<T>> _nodes;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;

        internal FeatureMap(IEnumerable<ClusterNode<T>> nodes, IFloatingPointFeatureExtractor<T> featureExtractor)
        {
            _nodes = nodes;
            _featureExtractor = featureExtractor;
            Features = featureExtractor.FeatureMetadata;
        }

        public ClusterNode<T> CreateNewNode(T member)
        {
            return new ClusterNode<T>(_featureExtractor, _featureExtractor.ExtractVector(member));
        }

        /// <summary>
        /// Returns the features that where used by the mappper
        /// </summary>
        public IEnumerable<IFeature> Features { get; private set; }

        public IEnumerator<ClusterNode<T>> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }
    }
}
