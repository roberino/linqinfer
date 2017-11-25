using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Maths
{
    [TestFixture]
    public class VectorFunctionTests
    {
        [Test]
        public void CreateScaleTransformation_GivenSampleMinMaxMean_ReturnsValidTransformation()
        {
            var minMaxAve = new MinMaxMeanVector(
                ColumnVector1D.Create(1, 2, 3),
                ColumnVector1D.Create(5, 6, 9));

            var input = ColumnVector1D.Create(5, 3, 8);

            var transform = minMaxAve
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
        public void MinOfEachDimension_GivenTwoColVectors_ReturnsExpectedValues()
        {
            var values = new[] { ColumnVector1D.Create(1, 5, 3), ColumnVector1D.Create(2, 4, 7) };

            var min = values.MinOfEachDimension();

            Assert.That(min[0], Is.EqualTo(1));
            Assert.That(min[1], Is.EqualTo(4));
            Assert.That(min[2], Is.EqualTo(3));
        }
    }
}
