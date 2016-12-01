using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MatrixTests : TestFixtureBase
    {
        [Test]
        public void GetHashCode_ReturnsSameValueForEquivMatrixes()
        {
            var m1 = new Matrix(new[]               
            {
                new [] { 3d, 5d },
                new [] { 6d, 8.7d },
            });

            var m2 = new Matrix(new[]
            {
                new [] { 3d, 5d },
                new [] { 6d, 8.7d },
            });

            var hash1 = m1.GetHashCode();
            var hash2 = m1.GetHashCode();

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void Covariance_2X2matrix_CovarAndVarianceValuesSetCorrectly()
        {
            var m = new Matrix(new[]
            {
                new [] { 3d, 7d },
                new [] { 2d, 4d },
            });

            var coVar = m.CovarianceMatrix;

            var stdDevX = (5 / 2d);
            var varX = Math.Pow(3 - stdDevX, 2) + Math.Pow(2 - stdDevX, 2);

            var stdDevY = (11 / 2d);
            var varY = Math.Pow(7 - stdDevY, 2) + Math.Pow(4 - stdDevY, 2);

            Assert.That(coVar[0, 0], Is.EqualTo(varX));
            Assert.That(coVar[1, 1], Is.EqualTo(varY));

            var coVarXY = (3 - 5 / 2d) * (7 - 11 / 2d) + (2 - 5 / 2d) * (4 - 11 / 2d);

            Assert.That(coVar[0, 1], Is.EqualTo(coVarXY));
            Assert.That(coVar[1, 0], Is.EqualTo(coVarXY));
        }

        [Test]
        public void MeanVector_ReturnsExpectedValues()
        {
            var m = new Matrix(new[]
            {
                new [] { 4.0d, 2d, 0.6d},
                new [] { 4.2d, 2.1d, 0.59d},
                new [] { 3.9d, 2.0d, 0.58d},
                new [] { 4.3d, 2.1d, 0.62d},
                new [] { 4.1d, 2.2d, 0.63d}
            });

            var mean = m.MeanVector;

            Assert.That(mean[0], IsAround(4.1d));
            Assert.That(mean[1], IsAround(2.08d));
            Assert.That(mean[2], IsAround(0.604d));

            var coVar = m.CovarianceMatrix;

            Assert.That(coVar[0, 0], Is.EqualTo(0.025));
            Assert.That(coVar[0, 1], Is.EqualTo(0.0075));
            Assert.That(coVar[0, 2], Is.EqualTo(0.00175));
            Assert.That(coVar[1, 0], Is.EqualTo(0.0075));
            Assert.That(coVar[1, 1], Is.EqualTo(0.0070));
            Assert.That(coVar[1, 2], Is.EqualTo(0.00135));
        }

        [Test]
        public void Rotate_ReturnsExpectedResult()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d},
                new [] { 5d, 6d}
            });

            var m2 = m1.Rotate();

            Assert.That(m2[0, 0], Is.EqualTo(5));
            Assert.That(m2[0, 1], Is.EqualTo(3));
            Assert.That(m2[0, 2], Is.EqualTo(1));
            Assert.That(m2[1, 0], Is.EqualTo(6));
            Assert.That(m2[1, 1], Is.EqualTo(4));
            Assert.That(m2[1, 2], Is.EqualTo(2));
        }

        [Test]
        public void AddOperator_ReturnsExpectedResult()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d}
            });

            var m2 = new Matrix(new[]
            {
                new [] { 7d, 8d},
                new [] { 9d, 10d}
            });

            var r = m1 + m2;

            Assert.That(r[0, 0], Is.EqualTo(8d));
            Assert.That(r[0, 1], Is.EqualTo(10d));
            Assert.That(r[1, 0], Is.EqualTo(12d));
            Assert.That(r[1, 1], Is.EqualTo(14d));
        }

        [Test]
        public void SubtractOperator_ReturnsExpectedResult()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d}
            });

            var m2 = new Matrix(new[]
            {
                new [] { 7d, 8d},
                new [] { 9d, 10d}
            });

            var r = m1 - m2;

            Assert.That(r[0, 0], Is.EqualTo(-6d));
            Assert.That(r[0, 1], Is.EqualTo(-6d));
            Assert.That(r[1, 0], Is.EqualTo(-6d));
            Assert.That(r[1, 1], Is.EqualTo(-6d));
        }

        [Test]
        public void MulitplyOperator_ReturnsExpectedResult()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d}
            });

            var m2 = new Matrix(new[]
            {
                new [] { 7d, 8d},
                new [] { 9d, 10d}
            });

            var r = m1 * m2;

            Assert.That(r[0, 0], Is.EqualTo(7d));
            Assert.That(r[0, 1], Is.EqualTo(16d));
            Assert.That(r[1, 0], Is.EqualTo(27d));
            Assert.That(r[1, 1], Is.EqualTo(40d));
        }

        [Test]
        public void MulitplyOperator_Vector_ReturnsExpectedResult()
        {
            var m = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d},
                new [] { 5d, 6d}
            });

            var v = new Vector(new[] { 5d, 6d });

            var r = m * v;

            Assert.That(r[0, 0], Is.EqualTo(5d));
            Assert.That(r[0, 1], Is.EqualTo(12d));
            Assert.That(r[1, 0], Is.EqualTo(15d));
            Assert.That(r[1, 1], Is.EqualTo(24d));
            Assert.That(r[2, 0], Is.EqualTo(25d));
            Assert.That(r[2, 1], Is.EqualTo(36d));
        }


        [Test]
        public void WidthAndHeight_ReturnExpectedValues()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 1d, 2d},
                new [] { 3d, 4d},
                new [] { 5d, 6d}
            });

            Assert.That(m1.Width, Is.EqualTo(2));
            Assert.That(m1.Height, Is.EqualTo(3));

            Assert.That(m1.ElementAt(0)[0], Is.EqualTo(1));
            Assert.That(m1.ElementAt(0)[1], Is.EqualTo(2));
            Assert.That(m1.ElementAt(1)[0], Is.EqualTo(3));
            Assert.That(m1.ElementAt(1)[1], Is.EqualTo(4));
            Assert.That(m1.ElementAt(2)[0], Is.EqualTo(5));
            Assert.That(m1.ElementAt(2)[1], Is.EqualTo(6));
            Assert.That(m1[2, 1], Is.EqualTo(6));
        }
    }
}