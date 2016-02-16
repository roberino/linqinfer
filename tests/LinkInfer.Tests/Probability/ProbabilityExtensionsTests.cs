using LinqInfer.Probability;
using NUnit.Framework;

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
    }
}
