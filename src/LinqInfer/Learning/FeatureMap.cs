using System.Collections;
using System.Collections.Generic;

namespace LinqInfer.Learning
{
    public class FeatureMap<T> : IEnumerable<ClusterNode<T>>
    {
        private readonly IEnumerable<ClusterNode<T>> _nodes;

        public FeatureMap(IEnumerable<ClusterNode<T>> nodes, IDictionary<string, int> featureLabels)
        {
            _nodes = nodes;
            FeatureLabels = featureLabels;
        }

        public IDictionary<string, int> FeatureLabels { get; private set; }

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
