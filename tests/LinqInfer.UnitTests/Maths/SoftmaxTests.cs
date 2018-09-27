using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class SoftmaxTests : TestFixtureBase
    {
        [Test]
        public void Apply_ReturnsCorrectTypeAndValues()
        {
            var softmax = new Softmax(3);

            var result = softmax.Apply(ColumnVector1D.Create(0.35, 0.78, 1.12));

            Assert.That(result, Is.InstanceOf<ColumnVector1D>());

            Assert.That(result[0], IsAround(0.2129));
            Assert.That(result[1], IsAround(0.32728));
            Assert.That(result[2], IsAround(0.45981));
        }
    }
}