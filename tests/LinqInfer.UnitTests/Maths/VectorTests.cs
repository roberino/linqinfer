using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorTests
    {
        [Test]
        public void Multiply_WhenGivenTwoEqualSizedVectors_ThenCorrectResultsReturned()
        {
            var vect1 = new Vector(new double[] { 1, 5, 6 });
            var vect2 = new Vector(new double[] { 2, 6, 2 });

            var result = vect1 * vect2;

            Assert.That(result.Size, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(2));
            Assert.That(result[1], Is.EqualTo(30));
            Assert.That(result[2], Is.EqualTo(12));
        }

        [Test]
        public void Multiply_WhenGivenVectorAndMatrix_ThenCorrectSizeVectorReturned()
        {
            var vect1 = new Vector(new double[] { 1, 5, 6 });
            var matrix1 = new Matrix(new[] { new[] { 2d, 6, 2 }, new[] { 7d, 2, 1 } });

            var result = vect1.MultiplyBy(matrix1);

            Assert.That(result.Size, Is.EqualTo(2));
        }
    }
}