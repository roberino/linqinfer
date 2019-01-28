using LinqInfer.Text;
using LinqInfer.Learning;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.IntegrationTests
{
    [TestFixture]
    public class Samples : TestFixtureBase
    {
        [TestCase(1)]
        [TestCase(2)]
        public void ShakespeareAsNOrderMarkovChain(byte order)
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var corpus = corpusStream.Tokenise().Where(t => t.Type == TokenType.Word || t.Type == TokenType.SentenceEnd || (t.Type == TokenType.Symbol && t.Text == "?"));

                var mk = corpus.AsMarkovChain(w => w.Type == TokenType.SentenceEnd || (w.Type == TokenType.Symbol && w.Text == "?"), order);

                foreach (var n in Enumerable.Range(0, 10))
                {
                    foreach (var w in mk.Simulate())
                    {
                        Console.Write(w.Text + " ");
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        [Test]
        public void ShakespeareAs4OrderMarkovChain()
        {
            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var corpus = corpusStream.Tokenise().Where(t => t.Type != TokenType.Space);

                var mk = corpus.AsMarkovChain(w => 
                    w.Type == TokenType.SentenceEnd || 
                    (w.Type == TokenType.Symbol && (w.Text == "?" || w.Text == ",")), 2, t => t.Text);

                foreach (var n in Enumerable.Range(0, 10))
                {
                    foreach (var w in mk.Simulate())
                    {
                        Console.Write(w.Text + " ");
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }
        }

        [Test]
        public void CombineWordSearchAsHypothesis()
        {
            var dict = new EnglishDictionary();

            var possibleMatches = dict.FindWordsLike("businesses").AsHypotheses();

            possibleMatches.Update(w => w.StartsWith("business") && !w.EndsWith("es") ? (1).OutOf(1) : (1).OutOf(2));

            var mostProbable = possibleMatches.MostProbable();

            Assert.That(mostProbable, Is.EqualTo("businessmen"));
        }
    }
}