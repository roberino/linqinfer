using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using LinqInfer.Maths.Geometry;

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

        public GraphExportMode ExportMode { get; set; }

        public async Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            Point3D? bounds = null,
            Point3D origin = default(Point3D),
            IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            if (!bounds.HasValue) bounds = new Point3D() { X = 100, Y = 100 };

            var width = bounds.Value.X;
            var height = bounds.Value.Y;

            double unitW = width / _nodes.Count();
            double unitH = height / 2;
            double maxRadius = _nodes.Max(n => n.CurrentRadius.GetValueOrDefault(0));

            int i = 1;

            foreach (var node in _nodes)
            {
                var x0 = unitW * i - 1 + unitW / 2;
                var y0 = unitH;

                var radius = maxRadius == 0 ? unitW / 2 : node.CurrentRadius.GetValueOrDefault(1) / maxRadius * unitW / 2;

                var vertex = await graph.FindOrCreateVertexAsync("N " + i++);

                var attribs = await vertex.GetAttributesAsync();

                attribs["currentRadius"] = node.CurrentRadius;
                attribs["initialRadius"] = node.InitialRadius;
                attribs["weights"] = node.Weights.ToJson();

                await vertex.SetPositionAndSizeAsync(x0, y0, 0, unitW / 2);
                await vertex.SetColourAsync(255, 0, 0);

                int m = 1;
                var members = node.GetMembers();

                var posCalc = GetPositionCalculator(origin, radius, node);

                foreach (var item in members)
                {
                    var memberVertex = await vertex.ConnectToAsync(GetLabelForMember(item.Key, i, m++), item.Value);

                    var memberAttribs = await memberVertex.GetAttributesAsync();

                    memberAttribs["count"] = item.Value;

                    var pos = posCalc(m, item.Key, item.Value);

                    await memberVertex.SetPositionAndSizeAsync(pos.Item1.X + x0, pos.Item1.Y + y0, 0, unitW / 4 * pos.Item2);
                    await memberVertex.SetColourAsync(0, 0, 0);
                }
            }

            await graph.SaveAsync();

            return graph;
        }

        public enum GraphExportMode
        {
            Schematic,
            Spatial1D,
            Spatial3D
        }

        private Func<int, T, int, Tuple<Point3D, double>> GetPositionCalculator(Point3D origin, double radius, ClusterNode<T> node)
        {
            var members = node.GetMembers();
            var count = members.Count;

            if (ExportMode == GraphExportMode.Spatial1D)
            {
                var dists = members.Select(n => new
                {
                    k = n.Key,
                    d = node.CalculateDifference(n.Key),
                    c = n.Value
                }).ToDictionary(m => m.k, m => m);

                var maxDist = dists.Max(d => d.Value.d);
                var maxCount = (double)members.Max(m => m.Value);

                return (i, m, c) =>
                {
                    var distRatio = dists[m].d / maxDist;
                    var r = distRatio * radius;
                    var angle = i / (double)count * 360d * Math.PI / 180d;
                    var x = r * Math.Sin(angle);
                    var y = r * Math.Cos(angle);

                    return new Tuple<Point3D, double>(origin + new Point3D()
                    {
                        X = x,
                        Y = y
                    }, c / maxCount);
                };
            }

            if (ExportMode == GraphExportMode.Spatial3D)
            {
                var pipe = members.Select(m => node.CalculateDifferenceVector(m.Key)).AsQueryable().CreatePipeline();
                var reduce = pipe.PrincipalComponentReduction(3);
                var fe = reduce.FeatureExtractor;
                var maxCount = (double)members.Max(m => m.Value);

                return (i, m, c) =>
                {
                    var vect = fe.ExtractColumnVector(node.CalculateDifferenceVector(m));

                    return new Tuple<Point3D, double>(origin + new Point3D()
                    {
                        X = vect[0] * radius,
                        Y = vect[1] * radius,
                        Z = vect[2] * radius
                    }, c / maxCount);
                };
            }

            return (i, m, c) =>
            {
                var angle = i / (double)count * 360d * Math.PI / 180d;
                var x = radius * Math.Sin(angle);
                var y = radius * Math.Cos(angle);

                return new Tuple<Point3D, double>(origin + new Point3D()
                {
                    X = x,
                    Y = y
                }, 1);
            };
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