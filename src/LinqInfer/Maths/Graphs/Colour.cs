namespace LinqInfer.Maths.Graphs
{
    public struct Colour
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public Colour AdjustLightness(float factor)
        {
            var r = factor * R;
            var g = factor * G;
            var b = factor * B;

            return new Colour()
            {
                R = (byte)(r > 255 ? 255 : r),
                G = (byte)(g > 255 ? 255 : g),
                B = (byte)(b > 255 ? 255 : b)
            };
        }

        public static readonly Colour Black = new Colour() { A = 255 };
        public static readonly Colour White = new Colour() { A = 255,  };
    }
}
