using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MinMaxMeanVectorTests
    {
        [Test]
        public void GivenSomeValues_CalculatesTheCorrectMinMaxAndMean()
        {
            var vect0 = ColumnVector1D.Create(1, 5, 12.3);
            var vect1 = ColumnVector1D.Create(14.1, 5.5, 11);
            var vect2 = ColumnVector1D.Create(-4, 2, 2.3);

            var minMaxMean = new[] { vect0, vect1, vect2 }.MinMaxAndMeanOfEachDimension();

            Assert.That(minMaxMean.Min.Equals(ColumnVector1D.Create(-4, 2, 2.3)));
            Assert.That(minMaxMean.Max.Equals(ColumnVector1D.Create(14.1, 5.5, 12.3)));
            Assert.That(minMaxMean.Mean.Equals(ColumnVector1D.Create(11.1d / 3d, 12.5d / 3d, 25.6d / 3d)));
        }
    }
}