using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public class AsyncContinuousBagOfWords
    {
        readonly ICorpus _corpus;
        readonly ISemanticSet _widerVocabulary;

        internal AsyncContinuousBagOfWords(ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null, int paddingSize = 1)
        {
            _corpus = ArgAssert.AssertNonNull(corpus, nameof(corpus));

            TargetVocabulary = ArgAssert.AssertNonNull(targetVocabulary, nameof(targetVocabulary));

            _widerVocabulary = widerVocabulary;
        }

        public ISemanticSet WiderVocabulary => _widerVocabulary ?? TargetVocabulary;

        public ISemanticSet TargetVocabulary { get; }

        public ITransformingAsyncBatchSource<SyntacticContext> GetNGramSource(int padding = 2)
        {
            var asyncEnum = _corpus
                .ReadBlocksAsync()
                .TransformEachBatch(t => new ContinuousBagOfWords(t, TargetVocabulary, _widerVocabulary).GetNGrams(padding).ToList());

            return asyncEnum;
        }

        public ITransformingAsyncBatchSource<BiGram> GetBiGramSource(int padding = 2)
        {
            return GetNGramSource(padding).SplitEachItem(
                c => c
                    .ContextualWords
                    .Select(w =>
                    new BiGram(w.NormalForm(), c.TargetWord.NormalForm())));
        }
    }
}