using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorTests
    {
        [Test]
        public void Multiply()
        {
            var vect1 = new Vector(new double[] { 1, 5, 6 });
            var vect2 = new Vector(new double[] { 2, 6, 2 });

            var result = vect1 * vect2;

            Assert.That(result[0], Is.EqualTo(2));
            Assert.That(result[1], Is.EqualTo(30));
            Assert.That(result[2], Is.EqualTo(12));
        }
    }
}
