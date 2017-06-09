using LinqInfer.Maths.Geometry;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public static class SpatialExtensions
    {
        /// <summary>
        /// Adjusts the size and position using the supplied functions
        /// </summary>
        public static async Task AdjustPositionAndSizeAsync<T, C>(this WeightedGraphNode<T, C> vertex, Func<Point3D, Point3D> positionAdjustment, Func<double, double> sizeAdjustment)
            where T : IEquatable<T> where C : IComparable<C>
        {
            var attribs = await vertex.GetAttributesAsync();

            var posAndSize = GetPositionAndSize(attribs);

            var originalPos = posAndSize.Item1;

            var position = positionAdjustment(originalPos);

            attribs["viz:position.x"] = position.X;
            attribs["viz:position.y"] = position.Y;
            attribs["viz:position.z"] = position.Z;


            attribs["viz:size.value"] = sizeAdjustment(posAndSize.Item2);
        }

        /// <summary>
        /// Sets the size, position and shape
        /// </summary>
        public static async Task SetPositionAndSizeAsync<T, C>(this WeightedGraphNode<T, C> vertex, Point3D position,
            double? size = null,
            string shape = null)
            where T : IEquatable<T> where C : IComparable<C>
        {
            var attribs = await vertex.GetAttributesAsync();

            attribs["viz:position.x"] = position.X;
            attribs["viz:position.y"] = position.Y;
            attribs["viz:position.z"] = position.Z;

            if (size.HasValue)
            {
                attribs["viz:size.value"] = size.Value;
            }

            if (!string.IsNullOrEmpty(shape))
            {
                attribs["viz:shape.value"] = shape;
            }
        }

        /// <summary>
        /// Sets the size, position and shape
        /// </summary>
        public static async Task SetPositionAndSizeAsync<T, C>(this WeightedGraphNode<T, C> vertex, double x, double y, double z,
            double? size = null,
            string shape = null)
            where T : IEquatable<T> where C : IComparable<C>
        {
            await SetPositionAndSizeAsync(vertex, new Point3D() { X = x, Y = y, Z = z }, size, shape);
        }

        /// <summary>
        /// Sets the colour of the node using RGB and alpha values
        /// </summary>
        public static async Task SetColourAsync<T, C>(this WeightedGraphNode<T, C> vertex, byte r, byte g, byte b, byte a = 255)
            where T : IEquatable<T> where C : IComparable<C>
        {
            var attribs = await vertex.GetAttributesAsync();
            
            attribs["viz:color.r"] = r;
            attribs["viz:color.g"] = g;
            attribs["viz:color.b"] = b;
            attribs["viz:color.a"] = a;
        }

        /// <summary>
        /// Scales all nodes within the graph
        /// </summary>
        public static async Task Scale<T, C>(this WeightedGraph<T, C> graph, Point3D scaleFactor, double scale = 1)
            where T : IEquatable<T> where C : IComparable<C>
        {
            await Transform(graph, o => o * scaleFactor, s => s * scale);
        }

        /// <summary>
        /// Moves all nodes within the graph
        /// </summary>
        public static async Task Move<T, C>(this WeightedGraph<T, C> graph, Point3D translation)
            where T : IEquatable<T> where C : IComparable<C>
        {
            await Transform(graph, o => o + translation, s => s);
        }

        /// <summary>
        /// Transforms all nodes within the graph
        /// </summary>
        public static async Task Transform<T, C>(this WeightedGraph<T, C> graph, Func<Point3D, Point3D> positionTransformation, Func<double, double> scaleTransformation)
           where T : IEquatable<T> where C : IComparable<C>
        {
            foreach (var vertex in await graph.FindAllVertexesAsync())
            {
                await AdjustPositionAndSizeAsync(vertex, positionTransformation, scaleTransformation);
            }

            await graph.SaveAsync();
        }

        /// <summary>
        /// Gets the bounding co-ordinates in 3D space of the graph
        /// </summary>
        public static async Task<Tuple<Point3D, Point3D>> GetBounds<T, C>(this WeightedGraph<T, C> graph)
          where T : IEquatable<T> where C : IComparable<C>
        {
            double xMax = 0;
            double yMax = 0;
            double zMax = 0;
            double xMin = 0;
            double yMin = 0;
            double zMin = 0;

            foreach (var vertex in await graph.FindAllVertexesAsync())
            {
                var posAndSize = GetPositionAndSize(await vertex.GetAttributesAsync());

                if (xMax < posAndSize.Item1.X) xMax = posAndSize.Item1.X;
                if (yMax < posAndSize.Item1.Y) yMax = posAndSize.Item1.Y;
                if (zMax < posAndSize.Item1.Z) zMax = posAndSize.Item1.Z;

                if (xMin > posAndSize.Item1.X) xMin = posAndSize.Item1.X;
                if (yMin > posAndSize.Item1.Y) yMin = posAndSize.Item1.Y;
                if (zMin > posAndSize.Item1.Z) zMin = posAndSize.Item1.Z;
            }

            return new Tuple<Point3D, Point3D>(new Point3D()
            {
                X = xMin,
                Y = yMin,
                Z = zMin
            },
            new Point3D()
            {
                X = xMax,
                Y = yMax,
                Z = zMax
            });
        }

        private static Tuple<Point3D, double> GetPositionAndSize(IDictionary<string, object> attribs)
        {
            var x = GetValueOrDefault(attribs, "viz:position.x", 0);
            var y = GetValueOrDefault(attribs, "viz:position.y", 0);
            var z = GetValueOrDefault(attribs, "viz:position.z", 0);
            var s = GetValueOrDefault(attribs, "viz:size.value", 0);

            var originalPos = new Point3D() { X = x, Y = y, Z = z };

            return new Tuple<Point3D, double>(originalPos, s);
        }

        private static T GetValueOrDefault<T>(IDictionary<string, object> values, string key, T defaultValue)
        {
            object val;

            if (!values.TryGetValue(key, out val)) return defaultValue;

            if (val == null || !(val is T)) return defaultValue;

            return (T)val;
        }
    }
}