using LinqInfer.Probability;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class HypoSetTests
    {
        [TestCase(40, 10, 20, 3, 5)]
        public void CookieJar_Example2(int total, int choc1, int choc2, int expectedNumerator, int expectedDenominator)
        {
            var hypos = P.Hypotheses(P.Of("Jar1").Is(1).OutOf(2), P.Of("Jar2").Is(1).OutOf(2));

            var vanilla_jar1 = total - choc1;
            var vanilla_jar2 = total - choc2;

            hypos.Update(vanilla_jar1.OutOf(total), vanilla_jar2.OutOf(total));

            var postProb = hypos.ProbabilityOf("Jar1");

            Assert.That(postProb.Numerator, Is.EqualTo(expectedNumerator));
            Assert.That(postProb.Denominator, Is.EqualTo(expectedDenominator));
        }

        [TestCase(40, 10, 20, 3, 5)]
        public void CookieJar_Example(int total, int choc1, int choc2, int expectedNumerator, int expectedDenominator)
        {
            var sample1 = new Sample<Cookie>("Jar1");
            var sample2 = new Sample<Cookie>("Jar2");
            var hypos = new HypoSet<Cookie>("All");

            hypos.Add(sample1, sample2);

            sample1.Add(total - choc1, n => new Cookie() { F = 'V' });
            sample1.Add(choc1, n => new Cookie() { F = 'C' });

            sample2.Add(total - choc2, n => new Cookie() { F = 'V' });
            sample2.Add(choc2, n => new Cookie() { F = 'C' });

            var choc = It.IsAny<Cookie>(c => c.F == 'C');
            var vani = It.IsAny<Cookie>(c => c.F == 'V');

            sample1.ProbabilityOfEvent(choc);
            sample2.ProbabilityOfEvent(choc);

            var postProb = hypos.PosterierProbability(sample1, vani);

            Assert.That(postProb.Numerator, Is.EqualTo(expectedNumerator));
            Assert.That(postProb.Denominator, Is.EqualTo(expectedDenominator));
        }

        [TestCase(40, 10, 20, 3, 5)]
        public void CookieJar_Example_Weighted(int total, int choc1, int choc2, int expectedNumerator, int expectedDenominator)
        {
            // Given we have 2 jars
            // jar one contains 10 choc and 30 vanilla
            // jar two contains 20 choc and 20 vanilla
            // what is the probability of drawing a 
            // vanilla cookie from jar 1

            var sample1 = new WeightedSample<Cookie>("Jar1");
            var sample2 = new WeightedSample<Cookie>("Jar2");
            var hypos = new HypoSet<Cookie>("All");

            hypos.Add(sample1, sample2);

            var chocx = new Cookie() { F = 'C' };
            var vanix = new Cookie() { F = 'V' };

            var choc = It.Is(chocx);
            var vani = It.Is(vanix);

            sample1[chocx] = choc1;
            sample1[vanix] = total - choc1;
            sample2[chocx] = choc2;
            sample2[vanix] = total - choc2;

            sample1.ProbabilityOfEvent(choc);
            sample2.ProbabilityOfEvent(choc);

            var postProb = hypos.PosterierProbability(sample1, vani);

            Assert.That(postProb.Numerator, Is.EqualTo(expectedNumerator));
            Assert.That(postProb.Denominator, Is.EqualTo(expectedDenominator));
        }

        private class Cookie
        {
            public char F;
        }
    }
}
