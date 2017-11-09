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

        public AsyncEnumerator<SyntacticContext> Stream()
        {
            var asyncEnum = _corpus
                .ReadBlocksAsync()
                .AsAsyncEnumerator()
                .TransformEachBatch(t => new ContinuousBagOfWords(t, _targetVocabulary, _widerVocabulary, _padding).ToList());

            return asyncEnum;
        }
    }
}