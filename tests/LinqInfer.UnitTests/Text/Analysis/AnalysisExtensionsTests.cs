using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.Analysis
{
    [TestFixture]
    public class AnalysisExtensionsTests
    {
        const int _numberOfCorpusItems = 22;
        readonly Corpus _testCorpus;
        readonly ISemanticSet _testVocab;

        public AnalysisExtensionsTests()
        {
            _testCorpus = new Corpus("a b c d e f g h i j k l m n o p q r s t u v w x y z".Tokenise());
            _testVocab = _testCorpus.ExtractKeyTerms();
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
        public async Task CreateContinuousBagOfWordsAsyncTrainingSet_GivenSimpleCorpus_ReturnsExpectedBatches()
        {
            var cbow = _testCorpus.CreateAsyncContinuousBagOfWords(_testVocab);
            var trainingData = cbow.AsNGramAsyncTrainingSet();

            int counter = 0;

            var data = await trainingData.Source.ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.EqualTo(_numberOfCorpusItems));
        }

        [Test]
        public async Task CreateAggregatedTrainingSetAsync_ReturnsTrainingSet()
        {
            var ct = CancellationToken.None;

            var trainingSet = await _testCorpus
                .CreateAsyncContinuousBagOfWords(_testVocab)
                .CreateAggregatedTrainingSetAsync(ct);

            var vects = await trainingSet.Source.ToMemoryAsync(ct);

            Assert.That(vects.Count, Is.EqualTo(_numberOfCorpusItems));
        }

        [Test]
        public void CreateContinuousBagOfWordsTrainingSet_GivenSimpleCorpus_ReturnsExpectedBatches()
        {
            var cbow = _testCorpus.CreateContinuousBagOfWords(_testVocab);
            var trainingData = cbow.AsNGramTrainingSet();
            var trainingBatches = trainingData.ExtractTrainingVectorBatches();

            Assert.That(trainingBatches.Count(), Is.EqualTo(1));
            Assert.That(trainingBatches.First().Count, Is.EqualTo(_numberOfCorpusItems)); // (26 - 4) * 4));
        }

        [Test]
        public void CreateContinuousBagOfWords_GivenSimpleCorpus_ReturnExpectedValues()
        {
            var cbow = _testCorpus.CreateContinuousBagOfWords(_testVocab);

            Assert.That(cbow.GetNGrams().Count(), Is.EqualTo(_numberOfCorpusItems));

            char c = 'c';

            foreach(var context in cbow.GetNGrams())
            {
                Console.WriteLine(context);

                var offset = 0;

                for (int i = 0; i < 5; i++)
                {
                    if (i == 2)
                    {
                        offset = -1;
                        continue; // skip context index
                    }

                    Assert.That(
                        context.ContextualWords.ElementAt(i + offset).Text.Length, Is.EqualTo(1));

                    Assert.That(
                        context.ContextualWords.ElementAt(i + offset).Text[0], 
                        Is.EqualTo(Offset(c, -2 + i)));
                }

                Assert.That(context.TargetWord.Text.Length, Is.EqualTo(1));
                Assert.That(context.TargetWord.Text[0], Is.EqualTo(c));

                c = Offset(c, 1);
            }
        }

        char Offset(char c, int o)
        {
            return (char)((int)c + o);
        }
    }
}
