using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
using LinqInfer.Maths.Geometry;
using LinqInfer.Maths;

namespace LinqInfer.Learning
{
    public class FeatureMap<T> : IEnumerable<ClusterNode<T>>, IHasNetworkTopology
    {
        private readonly IEnumerable<ClusterNode<T>> _nodes;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly ClusteringParameters _parameters;

        internal FeatureMap(IEnumerable<ClusterNode<T>> nodes, IFloatingPointFeatureExtractor<T> featureExtractor, ClusteringParameters parameters)
        {
            _nodes = nodes;
            _featureExtractor = featureExtractor;
            _parameters = parameters;

            Features = featureExtractor.FeatureMetadata;
        }

        public ClusterNode<T> CreateNewNode(T member)
        {
            return new ClusterNode<T>(_featureExtractor, new ColumnVector1D(_featureExtractor.ExtractVector(member)), _parameters);
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

        public GraphExportMode ExportMode
        {
            get
            {
                return _parameters.ExportMode;
            }
            set
            {
                _parameters.ExportMode = value;
            }
        }

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
                var radius = maxRadius == 0 ? unitW / 2 : node.CurrentRadius.GetValueOrDefault(1) / maxRadius * unitW / 2;

                var nodePos = GetNodePosition(origin, unitW, unitH, i, node);

                var vertex = await graph.FindOrCreateVertexAsync("N " + i++);

                var attribs = await vertex.GetAttributesAsync();

                attribs["currentRadius"] = node.CurrentRadius;
                attribs["initialRadius"] = node.InitialRadius;
                attribs["weights"] = node.Weights.ToJson();

                await vertex.SetPositionAndSizeAsync(nodePos.Item1.X, nodePos.Item1.Y, nodePos.Item1.Z, nodePos.Item2);
                await vertex.SetColourAsync(255, 0, 0);

                int m = 1;
                var members = node.GetMembers();

                var posCalc = GetMemberPositionCalculator(origin, nodePos.Item1, radius, node);

                foreach (var item in members)
                {
                    var memberVertex = await vertex.ConnectToAsync(GetLabelForMember(item.Key, i, m++), item.Value);

                    var memberAttribs = await memberVertex.GetAttributesAsync();

                    memberAttribs["count"] = item.Value;

                    var pos = posCalc(m, item.Key, item.Value);
                    var colour = _parameters.ExportColourPalette.GetColourByIndex(i);

                    await memberVertex.SetPositionAndSizeAsync(pos.Item1.X, pos.Item1.Y, pos.Item1.Z, unitW / 4 * pos.Item2);
                    await memberVertex.SetColourAsync(colour);
                }
            }

            await FinalisePosition(graph, origin, bounds.Value);

            await graph.SaveAsync();

            return graph;
        }

        private async Task FinalisePosition(WeightedGraph<string, double> graph, Point3D origin, Point3D bounds)
        {
            if (ExportMode == GraphExportMode.Spatial3D)
            {
                await graph.FitWithinRectangle(origin, bounds);
            }
        }

        private Tuple<Point3D, double> GetNodePosition(Point3D origin, double unitW, double unitH, int i, ClusterNode<T> node)
        {
            if (ExportMode == GraphExportMode.Spatial3D)
            {
                ColumnVector1D vect;

                if (_featureExtractor.VectorSize > 3)
                {
                    var members = node.GetMembers();
                    var pipe = members.Select(m => _featureExtractor.ExtractColumnVector(m.Key)).AsQueryable().CreatePipeline();
                    var reduce = pipe.PrincipalComponentReduction(3);
                    var fe = reduce.FeatureExtractor;

                    vect = fe.ExtractColumnVector(node.Weights);
                }
                else
                {
                    vect = node.Weights;
                }

                return new Tuple<Point3D, double>(origin + new Point3D()
                {
                    X = vect[0],
                    Y = vect.Size > 1 ? vect[1] : 0,
                    Z = vect.Size > 2 ? vect[2] : 0
                }, unitW);
            }

            return new Tuple<Point3D, double>(origin + new Point3D()
            {
                X = unitW * i - 1 + unitW / 2,
                Y = unitH
            }, unitW / 2);
        }

        private Func<int, T, int, Tuple<Point3D, double>> GetMemberPositionCalculator(Point3D origin, Point3D nodePos, double radius, ClusterNode<T> node)
        {
            var members = node.GetMembers();
            var count = members.Count;

            if (ExportMode == GraphExportMode.RelativeSchematic)
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

                    return new Tuple<Point3D, double>(origin + nodePos + new Point3D()
                    {
                        X = x,
                        Y = y
                    }, c / maxCount);
                };
            }

            if (ExportMode == GraphExportMode.Spatial3D)
            {
                Func<T, ColumnVector1D> extractor;

                var maxCount = (double)members.Max(m => m.Value);

                if (_featureExtractor.VectorSize > 3)
                {
                    var pipe = members.Select(m => _featureExtractor.ExtractColumnVector(m.Key)).AsQueryable().CreatePipeline();
                    var reduce = pipe.PrincipalComponentReduction(3);
                    var fe = reduce.FeatureExtractor;

                    extractor = v => fe.ExtractColumnVector(_featureExtractor.ExtractColumnVector(v));
                }
                else
                {
                    extractor = _featureExtractor.ExtractColumnVector;
                }

                return (i, m, c) =>
                {
                    var vect = extractor(m);

                    return new Tuple<Point3D, double>(origin + new Point3D()
                    {
                        X = vect[0],
                        Y = vect.Size > 1 ? vect[1] : 0,
                        Z = vect.Size > 2 ? vect[2] : 0
                    }, c / maxCount);
                };
            }

            return (i, m, c) =>
            {
                var angle = i / (double)count * 360d * Math.PI / 180d;
                var x = radius * Math.Sin(angle);
                var y = radius * Math.Cos(angle);

                return new Tuple<Point3D, double>(origin + nodePos + new Point3D()
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