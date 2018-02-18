using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MatrixTests : TestFixtureBase
    {
        [TestCase(10)]
        [TestCase(5)]
        [TestCase(1)]
        public void IdentityMatrix_ReturnsValidMatrx(int size)
        {
            var id = Matrix.IdentityMatrix(size);

            int y = 0; int x = 0;

            foreach(var row in id)
            {
                x = 0;

                foreach(var col in row)
                {
                    Assert.That(col, Is.EqualTo(x == y ? 1 : 0));
                    x++;
                }

                y++;
            }
        }

        [Test]
        public void DiagonalMatrix_FromVector_ReturnsExpectedValues()
        {
            var v = ColumnVector1D.Create(1, 23, 55, 8, 3);

            var d = Matrix.DiagonalMatrix(v);

            int y = 0; int x = 0;

            foreach (var row in d)
            {
                x = 0;

                foreach (var col in row)
                {
                    Assert.That(col, Is.EqualTo(x == y ? v[x] : 0));
                    x++;
                }

                y++;
            }
        }

        [Test]
        public void Transpose_TranslatedValueAsExpected()
        {
            var m = new Matrix(new[] {
                new [] { 1d, 2, 3},
                new [] { 4d, 5, 6}
            });

            var mc = new Matrix(new[] {
                new [] { 1d, 4},
                new [] { 2d, 5},
                new [] { 3d, 6}
            });

            var m2 = m.Transpose();

            Assert.That(mc.Equals(m2));
        }

        [Test]
        public void Rotate_Anticlockwise()
        {
            var m = new Matrix(new[] {
                new [] { 1d, 2d},
                new [] { 3d, 4d}
            });

            var mc = new Matrix(new[] {
                new [] { 2d, 4d},
                new [] { 1d, 3d}
            });

            var m2 = m.Rotate(false);

            Assert.That(m2[0, 0], Is.EqualTo(2));
            Assert.That(m2[0, 1], Is.EqualTo(4));
            Assert.That(m2[1, 0], Is.EqualTo(1));
            Assert.That(m2[1, 1], Is.EqualTo(3));
            Assert.That(m2.Equals(mc));
        }

        [Test]
        public void Modified_IsFiredWhenVectorDataChanges()
        {
            var m = new Matrix(new[]
            {
                new [] { 3d, 5, 2},
                new [] { 6d, 8.7, 4.3 },
                new [] { 7d, 4, 6 }
            });

            var ave = m.MeanVector;
            var covar = m.CovarianceMatrix;

            bool modified = false;

            m.Modified += (s, e) =>
            {
                modified = true;
            };

            ((Vector)m.Rows[0]).Apply(r => r * 12);

            Assert.That(modified);
            Assert.That(m.MeanVector.Equals(ave), Is.False);
            Assert.That(m.CovarianceMatrix.Equals(covar), Is.False);
        }

        [Test]
        public void MeanAjdust_NewMeanIsZero()
        {
            var m = new Matrix(new[]
            {
                new [] { 3d, 5, 2},
                new [] { 6d, 8.7, 4.3 },
                new [] { 7d, 4, 6 }
            });

            var ma = m.MeanAdjust();

            ma.MeanVector.ToList().ForEach(v =>
            {
                Assert.That(v, IsAround(0));
            });
        }

        [Test]
        public void MultiplicationOperator_OfColumnVector()
        {
            var m = new Matrix(new[]
            {
                new [] { 3d, 5, 2},
                new [] { 6d, 8.7, 4.3 }
            });

            var v = ColumnVector1D.Create(3, 6, 7);

            var mm = (m * v).ToColumnVector().AsMatrix();

            Assert.That(mm.Width, Is.EqualTo(1));
            Assert.That(mm.Height, Is.EqualTo(2));
            Assert.That(mm[0, 0], Is.EqualTo(3 * 3 + 5 * 6 + 2 * 7));
            Assert.That(mm[1, 0], Is.EqualTo(6 * 3 + 8.7d * 6 + 4.3d * 7));
        }

        [Test]
        public void MultiplicationOperator_OfRowMatrixAndColumnMatrix()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 2d, 1, 4 },
                new [] { 1d, 5, 2 }
            });

            var m2 = new Matrix(new[]
            {
                new [] { 3d, 2 },
                new [] { -1d, 4},
                new [] { 1d, 2}
            });

            var mm = m2 * m1;
            //var mm1 = Matrix.Multiply(m1, m2);
            //var mm2 = Matrix.Multiply(m2, m1);

            Assert.That(mm, Is.Not.Null);
        }


        [Test]
        public void MultiplicationOperator_OfColumnMatrixAndRowMatrix()
        {
            var m1 = new Matrix(new[]
            {
                new [] { 6d},
                new [] { 4d },
                new [] { 7d }
            });

            var m2 = new Matrix(new[]
            {
                new [] { 3d, 5, 2}
            });

            var mm = m1 * m2;
            var mm2 = m2 * m1;

            Assert.That(mm.Width, Is.EqualTo(3));
            Assert.That(mm.Height, Is.EqualTo(3));

            Assert.That(mm[0, 0], Is.EqualTo(6 * 3));
            Assert.That(mm[0, 1], Is.EqualTo(6 * 5));
            Assert.That(mm[0, 2], Is.EqualTo(6 * 2));

            Assert.That(mm[1, 0], Is.EqualTo(4 * 3));
            Assert.That(mm[1, 1], Is.EqualTo(4 * 5));
            Assert.That(mm[1, 2], Is.EqualTo(4 * 2));

            Assert.That(mm[2, 0], Is.EqualTo(7 * 3));
            Assert.That(mm[2, 1], Is.EqualTo(7 * 5));
            Assert.That(mm[2, 2], Is.EqualTo(7 * 2));
        }

        [Test]
        public void MultiplicationOperator_OfSquareMatrix()
        {
            var m = new Matrix(new[]
            {
                new [] { 3d, 5, 2},
                new [] { 6d, 8.7, 4.3 },
                new [] { 7d, 4, 6 }
            });

            var mm = m * m;

            Assert.That(mm.IsSquare);
            Assert.That(mm.Width, Is.EqualTo(m.Width));
            Assert.That(mm[0, 0], Is.EqualTo(3 * 3 + 5 * 6 + 2 * 7));
            Assert.That(mm[1, 0], Is.EqualTo(6 * 3 + 8.7d * 6 + 4.3d * 7));
            Assert.That(mm[2, 0], Is.EqualTo(7 * 3 + 4 * 6 + 6 * 7));
            Assert.That(mm[0, 1], Is.EqualTo(3 * 5 + 5 * 8.7d + 2 * 4));
            Assert.That(mm[1, 1], Is.EqualTo(6 * 5 + 8.7d * 8.7d + 4.3d * 4));
        }

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

            Assert.That(coVar[0, 0], IsAround(0.025));
            Assert.That(coVar[0, 1], IsAround(0.0075));
            Assert.That(coVar[0, 2], IsAround(0.00175));
            Assert.That(coVar[1, 0], IsAround(0.0075));
            Assert.That(coVar[1, 1], IsAround(0.0070));
            Assert.That(coVar[1, 2], IsAround(0.00135));
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