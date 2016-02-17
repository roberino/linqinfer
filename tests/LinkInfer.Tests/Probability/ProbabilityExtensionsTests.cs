using LinqInfer.Probability;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class ProbabilityExtensionsTests
    {
        [Test]
        public void AsSampleSpace_AreMutuallyExclusive_ReturnsTrue_ForUniqueItems()
        {
            var sample = TestData.CreateQueryablePirates().AsSampleSpace();

            Assert.That(sample.AreMutuallyExclusive(p => p.Age == 25, p => p.Gold == 1600));
        }

        [Test]
        public void AsSampleSpace_Count_ReturnsCorrectValue()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.Count(), Is.EqualTo(testData.ToList().Count));
        }

        [Test]
        public void AsSampleSpace_ProbabilityOfEvent_ReturnsCorrectValue()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();
            int count = testData.ToList().Count;
            var pExpected = new Fraction(2, count);
            var pActual = sample.ProbabilityOfEvent(p => p.Age == 25 || p.Gold == 1600);

            Assert.That(pActual, Is.EqualTo(pExpected));
            Assert.That(pActual.Value, Is.EqualTo(2f / count));
        }

        [Test]
        public void AsSampleSpace_IsExhaustive_ReturnsTrueForPredicateSelectingAll()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsExhaustive(p => p.Age > 0), Is.True);
        }

        [Test]
        public void AsSampleSpace_IsSimple_ReturnsTrueForPredicateSelectingOne()
        {
            var testData = TestData.CreateQueryablePirates();
            var sample = testData.AsSampleSpace();

            Assert.That(sample.IsSimple(p => p.Gold >= 1800), Is.True);
        }
    }
}
