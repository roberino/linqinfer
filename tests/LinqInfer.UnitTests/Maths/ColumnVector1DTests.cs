﻿using System;
using System.Linq;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class ColumnVector1DTests
    {
        [Test]
        public void CosineDistance_WhenGivenSameVector_ThenReturnsZero()
        {
            var vect1 = new ColumnVector1D(5, 2, 7);

            var dist = vect1.CosineDistance(vect1);

            Assert.That(dist, Is.EqualTo(0));
        }

        [Test]
        public void CosineDistance_WhenGivenSameValues_ThenReturnsZero()
        {
            var vect1 = new ColumnVector1D(5, 2, 7);
            var vect2 = new ColumnVector1D(5, 2, 7);

            var dist = vect1.CosineDistance(vect2);

            Assert.That(dist, Is.EqualTo(0));
        }

        [Test]
        public void CosineDistance_WhenGivenTwoVectors_ThenReturnsCorrectValue()
        {
            var vect1 = new ColumnVector1D(-4, 2, 7);
            var vect2 = new ColumnVector1D(-8, 12, 4);

            var dist = vect1.CosineDistance(vect2);

            Assert.That(dist, Is.EqualTo(0.32433607530782382));
        }

        [Test]
        public void HorizontalMultiply_WhenGivenMultiColMatrix_ThenExpectedVectorReturned()
        {
            var v1 = new ColumnVector1D(3d, 4.1d, 8d);
            var m1 = new Matrix(new[] { new[] { 91d, 12 }, new[] { 23.3d, 8 }, new[] { 10.8d, 6 } });

            var x = v1.HorizontalMultiply(m1);

            Assert.That(x.Size, Is.EqualTo(2));
            Assert.That(x[0], Is.EqualTo(3d * 91d + 4.1 * 23.3 + 8d * 10.8));
            Assert.That(x[1], Is.EqualTo(3d * 12d + 4.1 * 8d + 8d * 6d));
        }

        [Test]
        public void Split_And_Concat()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3);
            var vect2 = ColumnVector1D.Create(4, 5);

            var vect1and2 = vect1.Concat(vect2);

            Assert.That(vect1and2.Size, Is.EqualTo(5));
            Assert.That(vect1and2[0], Is.EqualTo(1));
            Assert.That(vect1and2[1], Is.EqualTo(2));
            Assert.That(vect1and2[2], Is.EqualTo(3));
            Assert.That(vect1and2[3], Is.EqualTo(4));
            Assert.That(vect1and2[4], Is.EqualTo(5));

            var vects = vect1and2.Split(3);
            var vect1a = vects[0];
            var vect2a = vects[1];

            Assert.That(vect1a.Equals(vect1));
            Assert.That(vect2a.Equals(vect2));
        }

        [Test]
        public void Range_ReturnsCorrectValues()
        {
            var vect1 = ColumnVector1D.Create(2, 6, 8, 16);
            var vect2 = ColumnVector1D.Create(16, 16, 16, 16);
            var range = vect1.Range(vect2, 4).ToList();

            Assert.That(range.Count, Is.EqualTo(4));
            Assert.That(range.First(), Is.EqualTo(vect1));
            Assert.That(range.Last(), Is.EqualTo(vect2));
            Assert.That(range[1][0], Is.EqualTo(((16 - 2) / 3d) + 2));
            Assert.That(range[1][1], Is.EqualTo(((16 - 6) / 3d) + 6));
            Assert.That(range[1][2], Is.EqualTo(((16 - 8) / 3d) + 8));
            Assert.That(range[1][3], Is.EqualTo(16));

            foreach (var bin in range)
            {
                Console.WriteLine(bin.ToCsv(2));
            }
        }

        [Test]
        public void Equals_ReturnsTrueForSameValues()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(1, 2, 3, 4);

            Assert.That(vect1.Equals(vect2));
        }

        [Test]
        public void Equals_ReturnsFalseForDifferentValues()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(1, 3, 2, 4);

            Assert.That(vect1.Equals(vect2), Is.False);
        }

        [Test]
        public void Multiply_Op_ReturnsExpectedOutput()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(5, 4, 3, 2);
            var vect3 = vect1 * vect2;

            Assert.That(vect3.Size, Is.EqualTo(4));
            Assert.That(vect3[0], Is.EqualTo(1 * 5));
            Assert.That(vect3[1], Is.EqualTo(2 * 4));
            Assert.That(vect3[2], Is.EqualTo(3 * 3));
            Assert.That(vect3[3], Is.EqualTo(4 * 2));
        }

        [Test]
        public void Addition_Op_ReturnsExpectedOutput()
        {
            var vect1 = ColumnVector1D.Create(1, 2, 3, 4);
            var vect2 = ColumnVector1D.Create(5, 4, 3, 2);
            var vect3 = vect1 + vect2;

            Assert.That(vect3.Size, Is.EqualTo(4));
            Assert.That(vect3[0], Is.EqualTo(1 + 5));
            Assert.That(vect3[1], Is.EqualTo(2 + 4));
            Assert.That(vect3[2], Is.EqualTo(3 + 3));
            Assert.That(vect3[3], Is.EqualTo(4 + 2));
        }
    }
}
