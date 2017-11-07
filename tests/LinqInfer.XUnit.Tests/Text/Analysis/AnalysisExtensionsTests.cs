using LinqInfer.Learning.Classification;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace LinqInfer.XUnit.Tests.Text.Analysis
{
    public class AnalysisExtensionsTests
    {
        private readonly ITestOutputHelper _output;

        public AnalysisExtensionsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void CreateContinuousBagOfWordsTrainingSet_ShouldNotBeNull()
        {
            var corpus = TestData.GetShakespeareCorpus();

            var targetVocab = (SemanticSet)"love hate";
            var widerVocab = corpus.ExtractKeyTerms(50);

            var trainingSet = corpus.CreateContinuousBagOfWordsTrainingSet(targetVocab, widerVocab, 100, 1);

            Assert.NotNull(trainingSet);
        }

        [Fact]
        public void CreateContinuousBagOfWordsTrainingSet2()
        {
            var corpus = TestData.GetShakespeareCorpus();

            var targetVocab = (SemanticSet)"love hate";
            var widerVocab = corpus.ExtractKeyTerms(5000);

            var trainingSet = corpus.CreateContinuousBagOfWords(targetVocab, widerVocab);

            foreach (var item in trainingSet)
            {
                foreach (var word in item.ContextualWords)
                {
                    _output.WriteLine("{0} ~ {1}", word.Text, item.TargetWord.Text);
                }
            }
        }

        [Fact]
        public void LinearClassifier()
        {
            var corpus = TestData.GetShakespeareCorpus();

            var targetVocab = corpus.ExtractKeyTerms(50);

            var trainingSet = corpus.CreateContinuousBagOfWordsTrainingSet(targetVocab, targetVocab, 500, 2);
            
            var linClassifier = new LinearClassifier(trainingSet.FeaturePipeline.VectorSize, trainingSet.OutputMapper.VectorSize);

            foreach(var batch in trainingSet.ExtractTrainingVectorBatches())
            {
                linClassifier.Train(batch.Where(v => !v.Input.ToColumnVector().IsZero));
            }
        }
    }
}