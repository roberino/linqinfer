using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public class AsyncContinuousBagOfWords
    {
        private readonly ICorpus _corpus;
        private readonly ISemanticSet _targetVocabulary;
        private readonly ISemanticSet _widerVocabulary;
        private readonly int _padding;

        internal AsyncContinuousBagOfWords(ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null, int paddingSize = 1)
        {
            ArgAssert.AssertGreaterThanZero(paddingSize, nameof(paddingSize));

            _corpus = corpus;
            _targetVocabulary = targetVocabulary;
            _widerVocabulary = widerVocabulary;
            _padding = paddingSize;
        }

        public IAsyncEnumerator<SyntacticContext> GetNGramSource()
        {
            var asyncEnum = _corpus
                .ReadBlocksAsync()
                .TransformEachBatch(t => new ContinuousBagOfWords(t, _targetVocabulary, _widerVocabulary, _padding).GetNGrams().ToList());

            return asyncEnum;
        }

        public IAsyncEnumerator<BiGram> GetBiGramSource()
        {
            return GetNGramSource().SplitEachItem(
                c => c
                    .ContextualWords
                    .Select(w =>
                    new BiGram()
                    {
                        Input = w.Text,
                        Output = c.TargetWord.Text
                    }));
        }
    }
}