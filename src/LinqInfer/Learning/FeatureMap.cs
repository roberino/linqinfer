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
        where T : class
    {
        readonly IEnumerable<ClusterNode<T>> _nodes;
        readonly ClusteringParameters _parameters;

        internal FeatureMap(IEnumerable<ClusterNode<T>> nodes, IFloatingPointFeatureExtractor<T> featureExtractor, ClusteringParameters parameters)
        {
            _nodes = nodes;
            _parameters = parameters;

            FeatureExtractor = featureExtractor;
        }

        public IFloatingPointFeatureExtractor<T> CreateFeatureExtractor()
        {
            return new FeatureMapDataExtractor<T>(this);
        }

        public Matrix ExportClusterWeights()
        {
            return new Matrix(_nodes.Select(n => n.Weights));
        }

        /// <summary>
        /// Returns the features that where used by the mappper
        /// </summary>
        public IEnumerable<IFeature> Features => FeatureExtractor.FeatureMetadata;

        internal IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }

        /// <summary>
        /// Sets the export mode
        /// </summary>
        public GraphExportMode ExportMode
        {
            get => _parameters.ExportMode;
            set => _parameters.ExportMode = value;
        }

        public IEnumerator<ClusterNode<T>> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        public async Task<WeightedGraph<string, double>> ExportNetworkTopologyAsync(
            VisualSettings visualSettings = null,
            IWeightedGraphStore<string, double> store = null)
        {
            var graph = new WeightedGraph<string, double>(store ?? new WeightedGraphInMemoryStore<string, double>(), (x, y) => x + y);

            var vs = visualSettings ?? new VisualSettings();
            var width = vs.Bounds.X;
            var height = vs.Bounds.Y;

            double unitW = width / _nodes.Count();
            double unitH = height / 2;
            double maxRadius = _nodes.Max(n => n.CurrentRadius.GetValueOrDefault(0));

            int i = 1;

            foreach (var node in _nodes)
            {
                var colour = vs.Palette.GetColourByIndex(i);

                var radius = maxRadius == 0 ? unitW / 2 : node.CurrentRadius.GetValueOrDefault(1) / maxRadius * unitW / 2;

                var nodePos = GetNodePosition(vs.Origin, unitW, unitH, i, node);

                var vertex = await graph.FindOrCreateVertexAsync("N " + i);

                var attribs = await vertex.GetAttributesAsync();

                attribs["currentRadius"] = node.CurrentRadius;
                attribs["initialRadius"] = node.InitialRadius;
                attribs["weights"] = node.Weights.ToJson();

                await vertex.SetPositionAndSizeAsync(nodePos.Item1.X, nodePos.Item1.Y, nodePos.Item1.Z, nodePos.Item2);
                await vertex.SetColourAsync(colour.AdjustLightness(0.5f));

                int m = 1;
                var members = node.GetMembers();

                var posCalc = GetMemberPositionCalculator(vs.Origin, nodePos.Item1, radius, node);

                foreach (var item in members)
                {
                    var memberVertex = await vertex.ConnectToAsync(_parameters.LabelFormatter(item.Key, i, m++), item.Value);

                    var memberAttribs = await memberVertex.GetAttributesAsync();

                    memberAttribs["count"] = item.Value;

                    var pos = posCalc(m, item.Key, item.Value);

                    await memberVertex.SetPositionAndSizeAsync(pos.Item1.X, pos.Item1.Y, pos.Item1.Z, unitW * pos.Item2);
                    await memberVertex.SetColourAsync(colour);
                }

                i++;
            }

            await FinalisePositionAsync(graph, vs.Origin, vs.Bounds);

            await graph.SaveAsync();

            return graph;
        }

        async Task FinalisePositionAsync(WeightedGraph<string, double> graph, Point3D origin, Point3D bounds)
        {
            if (ExportMode == GraphExportMode.Spatial3D)
            {
                await graph.FitWithinRectangle(origin, bounds);
            }
        }

        Tuple<Point3D, double> GetNodePosition(Point3D origin, double unitW, double unitH, int i, ClusterNode<T> node)
        {
            if (ExportMode == GraphExportMode.Spatial3D)
            {
                ColumnVector1D vect;

                if (FeatureExtractor.VectorSize > 3)
                {
                    var members = node.GetMembers();
                    var pipe = members.Select(m => FeatureExtractor.ExtractIVector(m.Key)).AsQueryable().CreatePipeline();
                    var reduce = pipe.PrincipalComponentReduction(3);
                    var fe = reduce.FeatureExtractor;

                    vect = fe.ExtractIVector(node.Weights).ToColumnVector();
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
            }, unitW);
        }

        Func<int, T, int, Tuple<Point3D, double>> GetMemberPositionCalculator(Point3D origin, Point3D nodePos, double radius, ClusterNode<T> node)
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
                Func<T, IVector> extractor;

                var maxCount = (double)members.Max(m => m.Value);

                if (FeatureExtractor.VectorSize > 3)
                {
                    var pipe = members.Select(m => FeatureExtractor.ExtractIVector(m.Key)).AsQueryable().CreatePipeline();
                    var reduce = pipe.PrincipalComponentReduction(3);
                    var fe = reduce.FeatureExtractor;

                    extractor = v => fe.ExtractIVector(FeatureExtractor.ExtractIVector(v));
                }
                else
                {
                    extractor = FeatureExtractor.ExtractIVector;
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
    }
}