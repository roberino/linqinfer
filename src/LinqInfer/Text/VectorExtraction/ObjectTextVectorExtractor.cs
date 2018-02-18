using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.VectorExtraction
{
    internal class ObjectTextVectorExtractor<T> : TextVectorExtractor, IFloatingPointFeatureExtractor<T> where T : class
    {
        private readonly Func<T, IEnumerable<IToken>> _tokeniser;

        public ObjectTextVectorExtractor(Func<T, IEnumerable<IToken>> tokeniser)
        {
            _tokeniser = tokeniser;
        }

        public ObjectTextVectorExtractor(Func<T, IEnumerable<IToken>> tokeniser, IEnumerable<string> words) : base(words, 0, false)
        {
            _tokeniser = tokeniser;
        }

        public ObjectTextVectorExtractor(Func<T, IEnumerable<IToken>> tokeniser, IEnumerable<string> words, int normalisingFrequency) : base(words, normalisingFrequency)
        {
            _tokeniser = tokeniser;
        }

        public ObjectTextVectorExtractor(Func<T, IEnumerable<IToken>> tokeniser, IEnumerable<string> words, int[] normalisingFrequencies) : base(words, normalisingFrequencies)
        {
            _tokeniser = tokeniser;
        }

        public ColumnVector1D ExtractColumnVector(T obj)
        {
            return ExtractColumnVector(_tokeniser(obj));
        }

        public IVector ExtractIVector(T obj)
        {
            return ExtractIVector(_tokeniser(obj));
        }

        public double[] ExtractVector(T obj)
        {
            return ExtractVector(_tokeniser(obj));
        }

        public double[] NormaliseUsing(IEnumerable<T> samples)
        {
            return NormaliseUsing(samples.Select(_tokeniser));
        }
    }
}