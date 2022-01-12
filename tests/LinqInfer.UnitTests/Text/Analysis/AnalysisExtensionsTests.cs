using LinqInfer.Data.Pipes;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Text.Analysis
{
    [TestFixture]
    public class AnalysisExtensionsTests
    {
        readonly ICorpus _testCorpus;
        readonly ISemanticSet _testVocab;
        readonly int _numberOfCorpusItems;
        readonly int _numberOfCorpusBlocks;

        public AnalysisExtensionsTests()
        {
            _testCorpus = TestData.CreateCorpus();
            _testVocab = _testCorpus.ExtractKeyTerms(200);
            _numberOfCorpusItems = _testCorpus.Words.Count();
            _numberOfCorpusBlocks = _testCorpus.Blocks.Sum(b => b.Count(w => _testVocab.IsDefined(w.NormalForm())) - 8);
        }

        [Test]
        public async Task CreateBiGramAsyncTrainingSet_ExtractVectors()
        {
            var cbow = _testCorpus.CreateAsyncContinuousBagOfWords(_testVocab);
            var trainingData = cbow.AsBiGramAsyncTrainingSet();
            var vectors = await trainingData.ExtractVectorsAsync(CancellationToken.None, 8);

            Assert.That(vectors, Is.Not.Null);
        }

        [Test]
        public async Task CreateContinuousBagOfWordsAsyncTrainingSet_GivenCorpus_ReturnsExpectedBatches()
        {
            const int contextPadding = 1;
            var cbow = _testCorpus.CreateAsyncContinuousBagOfWords(_testVocab);
            var trainingData = cbow.AsNGramAsyncTrainingSet(contextPadding);

            var data = await trainingData.Source.ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.GreaterThan(375));
        }

        [Test]
        public async Task CreateAggregatedTrainingSetAsync_ReturnsTrainingSet()
        {
            var ct = CancellationToken.None;

            var trainingSet = await _testCorpus
                .CreateAsyncContinuousBagOfWords(_testVocab)
                .CreateAggregatedTrainingSetAsync(ct);

            var vects = await trainingSet.Source.ToMemoryAsync(ct);

            Assert.That(vects.Count, Is.EqualTo(_testVocab.Count));
        }

        [Test]
        public void CreateContinuousBagOfWordsTrainingSet_GivenSimpleCorpus_ReturnsExpectedBatches()
        {
            const int contextPadding = 3;
            var cbow = _testCorpus.CreateContinuousBagOfWords(_testVocab);
            var trainingData = cbow.AsNGramTrainingSet(contextPadding);
            var trainingBatches = trainingData.ExtractTrainingVectorBatches();

            Assert.That(trainingBatches.Count(), Is.EqualTo(1));
            Assert.That(trainingBatches.First().Count, Is.GreaterThan(375)); // (26 - 4) * 4));
        }
    }
}
