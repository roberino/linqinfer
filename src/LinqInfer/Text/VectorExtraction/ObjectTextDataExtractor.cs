using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Text.VectorExtraction
{
    class ObjectTextDataExtractor<T> : TextDataExtractor, IVectorFeatureExtractor<T> where T : class
    {
        readonly Func<T, IEnumerable<IToken>> _tokeniser;

        public ObjectTextDataExtractor(Func<T, IEnumerable<IToken>> tokeniser)
        {
            _tokeniser = tokeniser;
        }

        public ObjectTextDataExtractor(Func<T, IEnumerable<IToken>> tokeniser, IEnumerable<string> words) : base(words, 0, false)
        {
            _tokeniser = tokeniser;
        }

        public ObjectTextDataExtractor(Func<T, IEnumerable<IToken>> tokeniser, IEnumerable<string> words, int[] normalisingFrequencies) : base(words, normalisingFrequencies)
        {
            _tokeniser = tokeniser;
        }

        public bool CanEncode(T obj) => CanEncode(_tokeniser(obj));

        public IVector ExtractIVector(T obj)
        {
            return ExtractIVector(_tokeniser(obj));
        }
    }
}