using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.Analysis
{
    [TestFixture]
    public class ContinuousBagOfWordsTests
    {
        const int _numberOfCorpusItems = 22;
        readonly Corpus _testCorpus;
        readonly ISemanticSet _testVocab;

        public ContinuousBagOfWordsTests()
        {
            _testCorpus = new Corpus("a b c d e f g h i j k l m n o p q r s t u v w x y z".Tokenise());
            _testVocab = _testCorpus.ExtractKeyTerms();
        }

        [Test]
        public void CreateContinuousBagOfWords_GivenSimpleCorpus_ReturnExpectedValues()
        {
            var cbow = _testCorpus.CreateContinuousBagOfWords(_testVocab);

            Assert.That(cbow.GetNGrams().Count(), Is.EqualTo(_numberOfCorpusItems));

            char c = 'c';

            foreach (var context in cbow.GetNGrams())
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

        static char Offset(char c, int o)
        {
            return (char)((int)c + o);
        }
    }
}
