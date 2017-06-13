using System.Collections.Generic;

namespace LinqInfer.Maths.Graphs
{
    public class ColourPalette
    {
        private readonly List<Colour> _colours;

        public ColourPalette()
        {
            _colours = new List<Colour>();
        }

        public virtual Colour GetColourByIndex(int index)
        {
            if (Random) return GetColour(200);

            if(_colours.Count > 0)
            {
                return _colours[index % _colours.Count];
            }

            return Colour.Black;
        }

        public void AddColour(Colour colour)
        {
            _colours.Add(colour);
            Random = false;
        }

        public void Clear()
        {
            _colours.Clear();
        }

        public bool Random { get; set; }

        private Colour GetColour(byte lightness)
        {
            return new Colour()
            {
                R = (byte)(Functions.Random(lightness) + 255 - lightness),
                G = (byte)(Functions.Random(lightness) + 255 - lightness),
                B = (byte)(Functions.Random(lightness) + 255 - lightness),
                A = 255
            };
        }
    }
}