﻿using LinqInfer.Learning;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text.Analysis
{
    [TestFixture]
    public class AnalysisExtensionsTests
    {
        private readonly Corpus _testCorpus;
        private readonly ISemanticSet _testVocab;

        public AnalysisExtensionsTests()
        {
            _testCorpus = new Corpus("a b c d e f g h i j k l m n o p q r s t u v w x y z".Tokenise());
            _testVocab = _testCorpus.ExtractKeyTerms();
        }

        [Test]
        public async Task CreateLinearClassifier_ReturnsVectors()
        {
            var trainingData = _testCorpus.CreateContinuousBagOfWordsAsyncTrainingSet(_testVocab);

            var classifier = await trainingData.CreateLinearClassifier();

            Assert.That(classifier.Vectors, Is.Not.Null);
        }

        [Test]
        public async Task CreateContinuousBagOfWordsAsyncTrainingSet_GivenSimpleCorpus_ReturnsExpectedBatches()
        {
            var trainingData = _testCorpus.CreateContinuousBagOfWordsAsyncTrainingSet(_testVocab);

            int counter = 0;

            await trainingData
                .ExtractInputOutputIVectorBatches()
                .ProcessUsing(p =>
                {
                    Assert.That(counter, Is.EqualTo(p.BatchNumber));
                    Assert.That(p.Items.Count, Is.EqualTo(22));

                    counter++;

                    return true;
                });

            Assert.That(counter, Is.EqualTo(1));
        }

        [Test]
        public void CreateContinuousBagOfWordsTrainingSet_GivenSimpleCorpus_ReturnsExpectedBatches()
        {
            var trainingData = _testCorpus.CreateContinuousBagOfWordsTrainingSet(_testVocab, _testVocab);
            var trainingBatches = trainingData.ExtractTrainingVectorBatches();

            Assert.That(trainingBatches.Count(), Is.EqualTo(1));
            Assert.That(trainingBatches.First().Count, Is.EqualTo(22)); // (26 - 4) * 4));
        }

        [Test]
        public void CreateContinuousBagOfWords_GivenSimpleCorpus_ReturnExpectedValues()
        {
            var cbow = _testCorpus.CreateContinuousBagOfWords(_testVocab);

            Assert.That(cbow.GetNGrams().Count(), Is.EqualTo(26 - 4));

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

        private char Offset(char c, int o)
        {
            return (char)((int)c + o);
        }
    }
}
