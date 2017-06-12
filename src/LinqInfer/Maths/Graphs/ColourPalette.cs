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
            if(_colours.Count > 0)
            {
                return _colours[index % _colours.Count];
            }

            return Colour.Black;
        }

        public void AddColour(Colour colour)
        {
            _colours.Add(colour);
        }

        public void Clear()
        {
            _colours.Clear();
        }
    }
}