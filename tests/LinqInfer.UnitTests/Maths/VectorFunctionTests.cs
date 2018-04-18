using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorFunctionTests
    {
        [Test]
        public void CreateScaleTransformation_GivenSampleMinMaxMean_ReturnsValidTransformation()
        {
            var minMax = new MinMaxVector(
                ColumnVector1D.Create(1, 2, 3),
                ColumnVector1D.Create(5, 6, 9));

            var input = ColumnVector1D.Create(5, 3, 8);

            var transform = minMax
                .CreateScaleTransformation(new Range(1, 0));

            var output = transform.Apply(input);

            Assert.That(output[0], Is.EqualTo(1d));
            Assert.That(output[1], Is.EqualTo(1d / 4d));
            Assert.That(output[2], Is.EqualTo(5d / 6d));
        }

        [Test]
        public void CreateCentreAndScaleTransformation_GivenSampleMinMaxMean_ReturnsValidTransformation()
        {
            var minMaxAve = new MinMaxMeanVector(
                ColumnVector1D.Create(1, 2, 3),
                ColumnVector1D.Create(5, 6, 9),
                ColumnVector1D.Create(3, 5, 6));

            var input = ColumnVector1D.Create(5, 3, 8);

            var transform = minMaxAve
                .CreateCentreAndScaleTransformation(new Range(1, 0));

            var output = transform.Apply(input);

            Assert.That(output[0], Is.EqualTo(1d));
            Assert.That(output[1], Is.EqualTo(1d / 4d));
            Assert.That(output[2], Is.EqualTo(5d / 6d));
        }

        [Test]
        public void CreateCentreAndScaleTransformation_ThenScalesAsExpected()
        {
            var vect0 = ColumnVector1D.Create(1, 5, 12.3);
            var vect1 = ColumnVector1D.Create(14.1, 5.5, 11);
            var vect2 = ColumnVector1D.Create(-4, 2, 2.3);

            var minMaxMean = new[] { vect0, vect1, vect2 }.MinMaxAndMeanOfEachDimension();

            var transform = minMaxMean.CreateCentreAndScaleTransformation();
            
            var transformedVects = new[] { transform.Apply(vect0), transform.Apply(vect1), transform.Apply(vect2) };
            var minMaxMeanT = transformedVects.MinMaxAndMeanOfEachDimension();
            var maxT = minMaxMeanT.Max.ToColumnVector();
            var minT = minMaxMeanT.Min.ToColumnVector();

            Assert.That(maxT.All(v => Math.Round(v, 6) == 1d), Is.True);
            Assert.That(minT.All(v => Math.Round(v, 6) == -1d), Is.True);
        }

        [Test]
        public void MinOfEachDimension_GivenTwoColVectors_ReturnsExpectedValues()
        {
            var values = new[] { ColumnVector1D.Create(1, 5, 3), ColumnVector1D.Create(2, 4, 7) };

            var min = values.MinOfEachDimension();

            Assert.That(min[0], Is.EqualTo(1));
            Assert.That(min[1], Is.EqualTo(4));
            Assert.That(min[2], Is.EqualTo(3));
        }

        [Test]
        public void MinMaxAndMeanOfEachDimension_GivenTwoColVectors_ReturnsExpectedValues()
        {
            var values = new[] { ColumnVector1D.Create(1, 5, 3), ColumnVector1D.Create(2, 4, 7) };

            var minMaxMean = values.MinMaxAndMeanOfEachDimension();

            Assert.That(minMaxMean.Min[0], Is.EqualTo(1));
            Assert.That(minMaxMean.Min[1], Is.EqualTo(4));
            Assert.That(minMaxMean.Min[2], Is.EqualTo(3));
            Assert.That(minMaxMean.Max[0], Is.EqualTo(2));
            Assert.That(minMaxMean.Max[1], Is.EqualTo(5));
            Assert.That(minMaxMean.Max[2], Is.EqualTo(7));
            Assert.That(minMaxMean.Mean[0], Is.EqualTo(1.5d));
            Assert.That(minMaxMean.Mean[1], Is.EqualTo(4.5d));
            Assert.That(minMaxMean.Mean[2], Is.EqualTo(5));
        }
    }
}