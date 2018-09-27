using System.Linq;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class OneOfNVectorTests
    {
        [Test]
        public void HorizontalMultiply_WhenGivenMatrixAndInactiveIndex_ThenZeroVectReturned()
        {
            var v1 = new OneOfNVector(2);
            var m1 = new Matrix(new[] { new[] { 91d, 43 }, new[] { 23.3d, 12.4 } });

            var x = v1.HorizontalMultiply(m1);

            Assert.That(x, Is.InstanceOf<ZeroVector>());
            Assert.That(x.Size, Is.EqualTo(2));
        }

        [Test]
        public void HorizontalMultiply_WhenGivenSingleColMatrix_ThenActiveRowReturned()
        {
            var v1 = new OneOfNVector(3, 1);
            var m1 = new Matrix(new[] { new[] { 91d }, new[] { 23.3d }, new[] { 10.8d } });

            var x = v1.HorizontalMultiply(m1);

            Assert.That(x.Size, Is.EqualTo(1));
            Assert.That(x[0], Is.EqualTo(23.3d));
        }

        [Test]
        public void HorizontalMultiply_WhenGivenMultiColMatrix_ThenActiveRowReturned()
        {
            var v1 = new OneOfNVector(3, 0);
            var m1 = new Matrix(new[] { new[] { 91d, 12 }, new[] { 23.3d, 8 }, new[] { 10.8d, 6 } });

            var x = v1.HorizontalMultiply(m1);

            Assert.That(x.Size, Is.EqualTo(2));
            Assert.That(x[0], Is.EqualTo(91d));
            Assert.That(x[1], Is.EqualTo(12d));
        }

        [Test]
        public void Equals_WhenEqualColumnAndOneOfNVector_ThenTrue()
        {
            var vect1 = new OneOfNVector(4, 3);

            var vect2 = vect1.ToColumnVector();

            Assert.That(vect1.Equals(vect2));
            Assert.That(vect2.Equals(vect1));
        }

        [Test]
        public void Equals_WhenEqualOneOfNVectors_ThenTrue()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 3);

            Assert.That(vect1.Equals(vect2));
            Assert.That(vect2.Equals(vect1));
        }

        [Test]
        public void Equals_WhenNonEqualOneOfNVectors_ThenFalse()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 2);

            Assert.That(vect1.Equals(vect2), Is.False);
        }

        [Test]
        public void Multiply_WhenTwoOneOfNVectorsOfDifferentValues_ThenReturnsZeroVector()
        {
            var vect1 = new OneOfNVector(4, 3);
            var vect2 = new OneOfNVector(4, 2);

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result, Is.InstanceOf<ZeroVector>());
            Assert.That(result.Size, Is.EqualTo(4));
            Assert.That(result.ToColumnVector().All(v => v == 0));
        }

        [Test]
        public void Multiply_WhenTwoOneOfNVectorsOfSameValue_ThenReturnsSameVector()
        {
            var vect1 = new OneOfNVector(4, 2);
            var vect2 = new OneOfNVector(4, 2);

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result, Is.InstanceOf<OneOfNVector>());
            Assert.That(result.Size, Is.EqualTo(4));
            Assert.That(result[1], Is.EqualTo(0));
            Assert.That(result[2], Is.EqualTo(1));
            Assert.That(result[3], Is.EqualTo(0));
            Assert.That(result[4], Is.EqualTo(0));
        }

        [Test]
        public void Multiply_WhenGivenMatrixAndVectorWithActiveIndex_ThenReturnsExpectedSizedVector()
        {
            var vect1 = new OneOfNVector(4, 2);
            var vect2 = new Matrix(new[] { new[] { 5d, 2, 3, 1 }, new[] { 2d, 8, 6, 9 } });

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result.Size, Is.EqualTo(2));
        }

        [Test]
        public void Multiply_WhenGivenMatrixAndVectorWithNoIndex_ThenReturnsZeroVector()
        {
            var vect1 = new OneOfNVector(4);
            var vect2 = new Matrix(new[] { new[] { 5d, 2, 3, 1 }, new[] { 2d, 8, 6, 9 } });

            var result = vect1.MultiplyBy(vect2);

            Assert.That(result, Is.InstanceOf<ZeroVector>());
            Assert.That(result.Size, Is.EqualTo(2));
        }
    }
}