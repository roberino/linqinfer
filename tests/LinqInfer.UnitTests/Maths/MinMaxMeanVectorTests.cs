using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class MinMaxMeanVectorTests
    {
        [Test]
        public void GivenSomeValues_CalculatesTheCorrectMinMaxAndMean()
        {
            var vect0 = ColumnVector1D.Create(1, 5, 12.3);
            var vect1 = ColumnVector1D.Create(14.1, 5.5, 11);
            var vect2 = ColumnVector1D.Create(-4, 2, 2.3);

            var minMaxMean = new[] { vect0, vect1, vect2 }.MinMaxAndMeanOfEachDimension();

            Assert.That(minMaxMean.Min.Equals(ColumnVector1D.Create(-4, 2, 2.3)));
            Assert.That(minMaxMean.Max.Equals(ColumnVector1D.Create(14.1, 5.5, 12.3)));
            Assert.That(minMaxMean.Mean.Equals(ColumnVector1D.Create(11.1d / 3d, 12.5d / 3d, 25.6d / 3d)));
        }

        [Test]
        public void GivenZeros_CalculatesWithoutError()
        {
            var vect0 = ColumnVector1D.Create(6, 5, 0);
            var vect1 = ColumnVector1D.Create(14.1, 5.5, 0);
            var vect2 = ColumnVector1D.Create(-4, 2, 0f);

            var minMaxMean = new[] { vect0, vect1, vect2 }.MinMaxAndMeanOfEachDimension();

            Assert.That(minMaxMean.Min[2], Is.EqualTo(0));
        }

        [Test]
        public void GivenTheSameValue_ConstructsWithoutArgError()
        {
            var vect0 = ColumnVector1D.Create(1, 5, 12.3);

            var minMaxMean = new MinMaxMeanVector(vect0, vect0.Clone(true), vect0.Clone(true));

            Assert.That(minMaxMean, Is.Not.Null);
        }

        [Test]
        public void GivenInValidValues_ThrowsArgError()
        {
            var min = ColumnVector1D.Create(1, 5, 12.3);
            var max = ColumnVector1D.Create(0, 3, 12.3);
            var mean = ColumnVector1D.Create(-1, 5, 12.3);

            Assert.Throws<ArgumentException>(() =>
            {
                new MinMaxMeanVector(min, max, mean);
            });
        }

        [Test]
        public void GivenDifferentSizedVectors_ThrowsArgError()
        {
            var min = ColumnVector1D.Create(1, 2, 12.3);
            var max = ColumnVector1D.Create(2, 3);
            var mean = ColumnVector1D.Create(1.5, 2.5, 5);

            Assert.Throws<ArgumentException>(() =>
            {
                new MinMaxMeanVector(min, max, mean);
            });
        }
    }
}