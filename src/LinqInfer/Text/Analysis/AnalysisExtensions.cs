using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Text.VectorExtraction;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    public static class AnalysisExtensions
    {
        public static async Task<IImportableExportableSemanticSet> ExtractKeyTermsAsync(this ICorpus corpus, CancellationToken cancellationToken)
        {
            var builder = new SemanticSetBuilder();

            await builder.AddAsync(corpus, cancellationToken);

            return builder.Build();
        }

        public static ContinuousBagOfWords CreateContinuousBagOfWords(this ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null, int contextPadding = 2)
        {
            var cbow = new ContinuousBagOfWords(corpus.Words, targetVocabulary, widerVocabulary ?? targetVocabulary, contextPadding);

            return cbow;
        }

        public static ITrainingSet<SyntacticContext, string> CreateContinuousBagOfWordsTrainingSet(this ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary, int sampleSize = 1000, int contextPadding = 2)
        {
            var cbow = CreateContinuousBagOfWords(corpus, targetVocabulary, widerVocabulary, contextPadding);

            var data = cbow.AsQueryable();

            var encoder = new OneHotTextEncoding<SyntacticContext>(widerVocabulary, t => t.ContextualWords.Select(w => w.Text.ToLowerInvariant()).ToArray());

            var pipeline = data.CreatePipeline(encoder);

            return pipeline.AsTrainingSet(t => t.TargetWord.Text);
        }

        public static AsyncContinuousBagOfWords CreateAsyncContinuousBagOfWords(this ICorpus corpus, ISemanticSet targetVocabulary, int contextPadding = 2)
        {
            return new AsyncContinuousBagOfWords(corpus, targetVocabulary, null, contextPadding);
        }

        public static IAsyncTrainingSet<SyntacticContext, string> CreateContinuousBagOfWordsAsyncTrainingSet(this ICorpus corpus, ISemanticSet targetVocabulary, int contextPadding = 2)
        {
            var cbow = CreateAsyncContinuousBagOfWords(corpus, targetVocabulary, contextPadding);

            var encoder = new OneHotTextEncoding<SyntacticContext>(targetVocabulary, t => t.ContextualWords.Select(w => w.Text.ToLowerInvariant()).ToArray());

            var pipeline = new AsyncFeatureProcessingPipeline<SyntacticContext>(cbow.GetEnumerator(), encoder);

            var omf = new OutputMapperFactory<WordPair, string>();

            var mapper = omf.Create(targetVocabulary.Words);

            return pipeline.AsTrainingSet(t => t.TargetWord.Text.ToLowerInvariant(), mapper);
        }
    }
}