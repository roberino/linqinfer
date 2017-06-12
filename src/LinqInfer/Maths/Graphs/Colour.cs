namespace LinqInfer.Maths.Graphs
{
    public struct Colour
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;

        public static readonly Colour Black = new Colour() { A = 255 };
        public static readonly Colour White = new Colour() { A = 255,  };
    }
}
