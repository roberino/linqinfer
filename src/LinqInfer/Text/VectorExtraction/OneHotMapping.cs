using LinqInfer.Maths;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.VectorExtraction
{
    class OneHotMapping
    {
        private readonly List<string> _vocabulary;

        public OneHotMapping(ISemanticSet vocabulary)
        {
            _vocabulary = vocabulary.Words.Select(w => w.ToLowerInvariant()).ToList();
        }

        public ColumnVector1D Map(IToken token)
        {
            var vect = new double[_vocabulary.Count];

            var index = _vocabulary.IndexOf(token.Text.ToLowerInvariant());

            if(index > -1)
            {
                vect[index] = 1d;
            }

            return new ColumnVector1D(vect);
        }
    }
}