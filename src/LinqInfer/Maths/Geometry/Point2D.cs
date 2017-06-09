namespace LinqInfer.Maths.Geometry
{
    public struct Point2D
    {
        public double X;

        public double Y;

        public static Point2D operator +(Point2D a, Point2D b)
        {
            return new Point2D()
            {
                X = a.X + b.X,
                Y = a.Y + b.Y
            };
        }

        public static Point2D operator -(Point2D a, Point2D b)
        {
            return new Point2D()
            {
                X = a.X - b.X,
                Y = a.Y - b.Y
            };
        }
    }
}