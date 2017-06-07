using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace LinqInfer.Learning
{
    public class FeatureMap<T> : IEnumerable<ClusterNode<T>>, IHasNetworkTopology
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

        public async Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(double width = 100, double height = 100, IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            double unitW = width / _nodes.Count();
            double unitH = height / 2;
            double maxRadius = Math.Max(_nodes.Max(n => n.CurrentRadius.GetValueOrDefault(0)), 1);

            int i = 1;

            foreach (var node in _nodes)
            {
                var x0 = unitW + i;
                var y0 = unitH;

                var radius = node.CurrentRadius.GetValueOrDefault(1) / maxRadius * unitW / 2;

                var vertex = await graph.FindOrCreateVertexAsync("N " + i++);

                var attribs = await vertex.GetAttributesAsync();

                attribs["currentRadius"] = node.CurrentRadius;
                attribs["initialRadius"] = node.InitialRadius;
                attribs["weights"] = node.Weights.ToJson();

                await vertex.SetPositionAndSizeAsync(x0, y0, 0, unitW / 2);
                await vertex.SetColourAsync(255, 0, 0);

                int m = 1;
                var members = node.GetMembers();

                foreach (var item in members)
                {
                    var angle = m / (double)members.Count * 360d * Math.PI / 180d;
                    var x = radius * Math.Sin(angle);
                    var y = radius * Math.Cos(angle);

                    var memberVertex = await vertex.ConnectToAsync(GetLabelForMember(item.Key, i, m++), item.Value);

                    await memberVertex.SetPositionAndSizeAsync(x + x0, y + y0, 0, unitW / 4);
                    await vertex.SetColourAsync(0, 0, 0);
                }
            }

            await graph.SaveAsync();

            return graph;
        }

        private string GetLabelForMember(T member, int i, int j)
        {
            if (Type.GetTypeCode(typeof(T)) == TypeCode.Object)
            {
                return i + "." + j;
            }

            return member.ToString();
        }
    }
}