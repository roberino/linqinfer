using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class MultiVariateKernelDensityEstimatorTests
    {
        [Test]
        public void SampleCompare()
        {
            var kde = new MultiVariateKernelDensityEstimator(0.02f);

            DensityEstimationTests.RunSampleTest(kde, x => ColumnVector1D.Create(x.Value));
        }

        [Test]
        public void MultiVariateNormalKernel_ReturnsExpectedResults()
        {
            var vector1 = ColumnVector1D.Create(1, 2, 3, 3, 4, 4, 5, 5, 5, 6, 6, 7, 7, 8, 8, 9);
            var vector2 = ColumnVector1D.Create(1, 2, 3, 3, 4, 4, 5, 5, 5, 5, 6, 7, 7, 8, 8, 9);

            var normF = MultiVariateKernelDensityEstimator.MultiVariateNormalKernel(new[] { vector1, vector2 }, 2);

            var p = normF(ColumnVector1D.Create(2, 2, 3, 3, 4, 5, 5, 5, 5, 6, 6, 7, 8, 8, 8, 9));

            Assert.That(p > 0);
            Assert.That(p < 9);
        }
    }
}