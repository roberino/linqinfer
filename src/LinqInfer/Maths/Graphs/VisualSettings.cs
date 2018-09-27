using LinqInfer.Maths.Geometry;

namespace LinqInfer.Maths.Graphs
{
    public sealed class VisualSettings
    {
        public VisualSettings()
        {
            Palette = new ColourPalette()
            {
                Random = true
            };

            Bounds = new Point3D() { X = 100, Y = 100 };
        }

        public VisualSettings(Rectangle dimensions, ColourPalette palette = null)
        {
            Palette = palette ?? new ColourPalette()
            {
                Random = true
            };

            Origin = new Point3D() { X = dimensions.X, Y = dimensions.Y };
            Bounds = new Point3D() { X = dimensions.Width, Y = dimensions.Height };
        }

        public VisualSettings(Point3D bounds, Point3D? origin = null, ColourPalette palette = null)
        {
            Palette = palette ?? new ColourPalette()
            {
                Random = true
            };

            Origin = origin.GetValueOrDefault();
            Bounds = bounds;
        }

        public Point3D Origin { get; }

        public Point3D Bounds { get; }

        public ColourPalette Palette { get; }
    }
}