using LinqInfer.Probability;
using NUnit.Framework;

namespace LinqInfer.Tests.Probability
{
    [TestFixture]
    public class KernelDensityEstimatorTests
    {
        [Test]
        public void SampleCompare()
        {
            var kde = new KernelDensityEstimator(0.02f);

            //DensityEstimationTests.RunSampleTest(kde, x => x);
            DensityEstimationTests.RunSampleTest(kde, x => ColumnVector1D.Create(x.Value));
        }
    }
}
