using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorFunctionTests
    {
        [Test]
        public void MinOfEachDimension_GivenTwoColVectors_ReturnsExpectedValues()
        {
            var values = new[] { ColumnVector1D.Create(1, 5, 3), ColumnVector1D.Create(2, 4, 7) };

            var min = values.MinOfEachDimension();

            Assert.That(min[0], Is.EqualTo(1));
            Assert.That(min[1], Is.EqualTo(4));
            Assert.That(min[2], Is.EqualTo(3));
        }
    }
}
