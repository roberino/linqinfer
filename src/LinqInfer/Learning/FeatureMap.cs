using LinqInfer.Learning.Features;
using System.Collections;
using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public class FeatureMap<T> : IEnumerable<ClusterNode<T>>
    {
        private readonly IEnumerable<ClusterNode<T>> _nodes;

        internal FeatureMap(IEnumerable<ClusterNode<T>> nodes, IEnumerable<IFeature> features)
        {
            _nodes = nodes;
            Features = features;
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
