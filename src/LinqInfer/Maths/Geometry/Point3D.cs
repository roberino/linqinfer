namespace LinqInfer.Maths.Geometry
{
    public struct Point3D
    {
        public double X;

        public double Y;

        public double Z;

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }

        public static Point3D operator +(Point3D a, Point3D b)
        {
            return new Point3D()
            {
                X = a.X + b.X,
                Y = a.Y + b.Y,
                Z = a.Z + b.Z
            };
        }

        public static Point3D operator -(Point3D a, Point3D b)
        {
            return new Point3D()
            {
                X = a.X - b.X,
                Y = a.Y - b.Y,
                Z = a.Z - b.Z
            };
        }

        public static Point3D operator *(Point3D a, Point3D b)
        {
            return new Point3D()
            {
                X = a.X * b.X,
                Y = a.Y * b.Y,
                Z = a.Z * b.Z
            };
        }

        public static Point3D operator /(Point3D a, Point3D b)
        {
            return new Point3D()
            {
                X = a.X == 0 ? 0 : a.X / b.X,
                Y = a.Y == 0 ? 0 : a.Y / b.Y,
                Z = a.Z == 0 ? 0 : a.Z / b.Z
            };
        }
    }
}