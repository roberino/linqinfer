using System;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public static class SpatialExtensions
    {
        public static async Task SetPositionAndSizeAsync<T, C>(this WeightedGraphNode<T, C> vertex, double x, double y, double z, 
            double? size = null,
            string shape = null)
            where T : IEquatable<T> where C : IComparable<C>
        {
            var attribs = await vertex.GetAttributesAsync();

            attribs["viz:position.x"] = x;
            attribs["viz:position.y"] = y;
            attribs["viz:position.z"] = z;

            if (size.HasValue)
            {
                attribs["viz:size.value"] = size.Value;
            }

            if (!string.IsNullOrEmpty(shape))
            {
                attribs["viz:shape.value"] = shape;
            }
        }

        public static async Task SetColourAsync<T, C>(this WeightedGraphNode<T, C> vertex, byte r, byte g, byte b, byte a = 255)
            where T : IEquatable<T> where C : IComparable<C>
        {
            var attribs = await vertex.GetAttributesAsync();
            
            attribs["viz:color.r"] = r;
            attribs["viz:color.g"] = g;
            attribs["viz:color.b"] = b;
            attribs["viz:color.a"] = a;
        }
    }
}