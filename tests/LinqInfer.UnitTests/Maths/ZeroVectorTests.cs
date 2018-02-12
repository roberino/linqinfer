using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class ZeroVectorTests
    {
        [Test]
        public void HorizontalMultiply_WhenGivenMatrixAndZeroVector_ThenZeroVectReturned()
        {
            var v1 = new ZeroVector(2);
            var m1 = new Matrix(new[] { new[] { 1d, 4 }, new[] { 2.9d, 2.4 } });

            var x = v1.HorizontalMultiply(m1);

            Assert.That(x, Is.InstanceOf<ZeroVector>());
            Assert.That(x.Size, Is.EqualTo(2));
        }

        [Test]
        public void ToColumnVector_WhenCalled_ThenReturnsCorrectSizeAndZeroValues()
        {
            var v1 = new ZeroVector(81);

            var data = v1.ToColumnVector();

            Assert.That(data.Size, Is.EqualTo(81));

            Assert.True(data.All(v => v == 0));
        }
    }
}
