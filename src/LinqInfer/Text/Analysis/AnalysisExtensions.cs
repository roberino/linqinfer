using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Text.VectorExtraction;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    public static class AnalysisExtensions
    {
        public static ContinuousBagOfWords CreateContinuousBagOfWords(this ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary = null, int contextPadding = 2)
        {
            var cbow = new ContinuousBagOfWords(corpus.Words, targetVocabulary, widerVocabulary ?? targetVocabulary, contextPadding);

            return cbow;
        }

        public static ITrainingSet<WordPair, string> CreateContinuousBagOfWordsTrainingSet(this ICorpus corpus, int targetVocabularySize = 256, int superVocabularyFactor = 4, int sampleSize = 1000, int contextPadding = 2)
        {
            var index = new DocumentIndex();
            int i = 0;

            index.IndexDocuments(corpus
                .Blocks
                .Select(b => new TokenisedTextDocument((i++).ToString(), b)));

            var keyTermsSuperset = index.ExtractKeyTerms(targetVocabularySize * superVocabularyFactor);
            var targetVocab = index.ExtractKeyTerms(targetVocabularySize);

            return CreateContinuousBagOfWordsTrainingSet(corpus, targetVocab, keyTermsSuperset, sampleSize, contextPadding);
        }

        public static ITrainingSet<WordPair, string> CreateContinuousBagOfWordsTrainingSet(this ICorpus corpus, ISemanticSet targetVocabulary, ISemanticSet widerVocabulary, int sampleSize = 1000, int contextPadding = 2)
        {
            var cbow = CreateContinuousBagOfWords(corpus, targetVocabulary, widerVocabulary, contextPadding);

            var data = cbow.SelectMany(c =>
                    c.ContextualWords
                    .Select(w => new WordPair() { WordA = w.Text, WordB = c.TargetWord.Text })
                   )
                   .Take(sampleSize);

            var encoder = new OneHotTextEncoding<WordPair>(widerVocabulary, t => t.WordA);

            var pipeline = data.AsQueryable().CreatePipeline(encoder);

            return pipeline.AsTrainingSet(t => t.WordB);
        }

        public static IAsyncTrainingSet<WordPair, string> CreateContinuousBagOfWordsAsyncTrainingSet(this ICorpus corpus, ISemanticSet targetVocabulary, int sampleSize = 1000, int contextPadding = 2)
        {
            var cbow = new AsyncContinuousBagOfWords(corpus, targetVocabulary, null, contextPadding);
        
            var encoder = new OneHotTextEncoding<WordPair>(targetVocabulary, t => t.WordA);

            var pipeline = new AsyncFeatureProcessingPipeline<WordPair>(cbow.StreamPairs(), encoder);

            var omf = new OutputMapperFactory<WordPair, string>();

            var mapper = omf.Create(targetVocabulary.Words);

            return pipeline.AsTrainingSet(t => t.WordB, mapper);
        }
    }
}