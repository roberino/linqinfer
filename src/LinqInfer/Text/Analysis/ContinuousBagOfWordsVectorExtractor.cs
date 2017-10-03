using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Text.Analysis
{
    public class ContinuousBagOfWordsVectorExtractor
    {
        private readonly ISemanticSet _vocabulary;

        public ContinuousBagOfWordsVectorExtractor(ISemanticSet vocabulary)
        {
            _vocabulary = vocabulary;
        }

        public Vector Extract(WordContext context)
        {
            return null;
        }
    }
}
