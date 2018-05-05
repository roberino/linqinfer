using System;
using System.Linq;
using LinqInfer.Data.Serialisation;
using LinqInfer.Maths.Probability;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class MarkovChainTests
    {
        [Test]
        public void AddSequence_and_Export()
        {
            var mkc = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence("abcjholsdgkjhjjkkklkjssssdf");

            var xml = mkc.ExportAsXml();

            Console.Write(xml.ToString());
        }

        [Test]
        public void AddSequence_ToVectorDoc_FromVectorDoc()
        {
            var mkc = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence("abcjholsdgkjhjjkkklkjssssdf");

            var xml = mkc.ToDataDocument().ExportAsXml();

            var doc = new PortableDataDocument(xml);

            var mkc2 = new DiscreteMarkovChain<char>(doc);

            var x = mkc.GetPriorFrequencies('k');
            var y = mkc2.GetPriorFrequencies('k');

            Console.WriteLine(xml);

            Assert.That(x, Is.EqualTo(y));
            Assert.That(mkc.Order, Is.EqualTo(mkc2.Order));
        }

        [TestCase("abdadead", 'a', 'd', 1, 2)]
        [TestCase("abdadead", 'a', 'e', 1, 2)]
        [TestCase("abbgbngb", 'b', 'g', 2, 3)]
        public void AddSequence_and_GetPriorFrequencies_ReturnsCorrectFrequency(string sequence, char test, char assertChar, int assertFreq, int totalCount)
        {
            var mkc = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence(sequence);

            var freq = mkc.GetPriorFrequencies(test);

            Assert.That(freq.Count, Is.EqualTo(totalCount));
            Assert.That(freq[assertChar], Is.EqualTo(assertFreq));
        }

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
        public void AddSequence_and_Prune()
        {
            var mkc = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence("a1b1c1b2c2b3");

            Assert.That(mkc.GetFrequencies('b').Count, Is.EqualTo(3));
            Assert.That(mkc.GetFrequencies('a').Count, Is.EqualTo(1));

            mkc.Prune(3);

            Assert.That(mkc.GetFrequencies('b').Count, Is.EqualTo(3));
            Assert.That(mkc.GetFrequencies('a').Count, Is.EqualTo(0));
        }

        [Test]
        public void AddSequence_and_Merge_WithOther()
        {
            var mkc = new DiscreteMarkovChain<char>();
            var mkc2 = new DiscreteMarkovChain<char>();

            mkc.AnalyseSequence("a1b1c1b2c2b3");
            mkc2.AnalyseSequence("b5b1");

            Assert.That(mkc.GetFrequencies('b').Count, Is.EqualTo(3));
            Assert.That(mkc.GetFrequencies('b')['1'], Is.EqualTo(1));

            mkc.Merge(mkc2);

            Assert.That(mkc.GetFrequencies('b').Count, Is.EqualTo(4));
            Assert.That(mkc.GetFrequencies('b')['1'], Is.EqualTo(2));
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
        [Category("BuildOmit")]
        public void AddCharSequence_Simulate()
        {
            var mkc = new DiscreteMarkovChain<char>(2);

            var seq = "abcabdabcabcabn";

            mkc.AnalyseSequence(seq);

            var simulation = mkc.Simulate('b').ToList();

            Assert.That(simulation.Count > 0);
            Assert.That(simulation.All(c => seq.IndexOf(c) > -1));
        }

        [Test]
        [Ignore("Known issue")]
        public void AddIntSequence_Simulate()
        {
            var mkc = new DiscreteMarkovChain<int>(2);

            var seq = new[] { 1, 3, 3, 2, 5, 6, 4, 3, 2, 6, 9, 7, 2, 9, 1, 9 };

            mkc.AnalyseSequence(seq);

            var simulation = mkc.Simulate(5).ToList();

            Assert.That(simulation.Count > 0);
            Assert.That(simulation.All(c => seq.Any(x => x == c)));
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
