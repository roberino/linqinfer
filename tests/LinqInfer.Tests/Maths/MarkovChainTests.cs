using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MarkovChainTests
    {
        [TestCase("abdadead", 'a', 'd', 2, 2)]
        [TestCase("abdadead", 'a', 'b', 1, 2)]
        [TestCase("abdadead", 'b', 'd', 1, 1)]
        [TestCase("abdadead", 'd', 'e', 1, 2)]
        public void AddSequence_and_GetFrequencies_ReturnsCorrectFrequency(string sequence, char test, char assertChar, int assertFreq, int totalCount)
        {
            var mkc = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence(sequence);

            var freq = mkc.GetFrequencies(test);

            Assert.That(freq.Count, Is.EqualTo(totalCount));
            Assert.That(freq[assertChar], Is.EqualTo(assertFreq));
        }

        [TestCase("abdadead", 'a', 'd', 2, 2)]
        [TestCase("abdadead", 'a', 'b', 1, 2)]
        [TestCase("abdadead", 'b', 'd', 1, 1)]
        [TestCase("abdadead", 'd', 'e', 1, 2)]
        public void SecondOrder_AddSequence_and_GetFrequencies_ReturnsCorrectFrequency(string sequence, char test, char assertChar, int assertFreq, int totalCount)
        {
            var mkc = new DiscreteMarkovChain<char>(2);

            mkc.AnalyseSequence(sequence);

            var freq = mkc.GetFrequencies(test);

            Assert.That(freq.Count, Is.EqualTo(totalCount));
            Assert.That(freq[assertChar], Is.EqualTo(assertFreq));
        }

        [Test]
        public void AddSequence_GetFrequencies_ReturnsExpectedValues()
        {
            var mkc = new DiscreteMarkovChain<char>(2);

            mkc.AnalyseSequence("abcabdabcabcabn");

            var freq = mkc.GetFrequencies("ab");

            Assert.That(freq.Count, Is.EqualTo(3));
            Assert.That(freq['c'], Is.EqualTo(3));
            Assert.That(freq['d'], Is.EqualTo(1));
            Assert.That(freq['n'], Is.EqualTo(1));
        }

        [Test]
        public void AddSequence_Simulate()
        {
            var mkc = new DiscreteMarkovChain<char>(2);

            var seq = "abcabdabcabcabn";

            mkc.AnalyseSequence(seq);

            var simulation = mkc.Simulate('b').ToList();

            Assert.That(simulation.Count > 0);
            Assert.That(simulation.All(c => seq.IndexOf(c) > -1));
        }

        [Test]
        public void AddSequence_ProbabilityOf()
        {
            var mkc = new DiscreteMarkovChain<char>(2);

            var seq = "abcabdabcabcabn";
            
            mkc.AnalyseSequence(seq);

            var p = mkc.ProbabilityOfEvent("ab", 'c');
            
            Assert.That(p.Value, Is.EqualTo(3d / 5d));
        }
    }
}
