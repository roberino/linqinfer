using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class MultiStrategyFeatureExtractorTests
    {
        [Test]
        public void ExtractIVector_GivenSingleInnerFeatureExtractor_ReturnsExpectedVector()
        {
            var fe0 = new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 1.67d, 2.5d }, 2);

            var multiStrategyFeatureExtractor1 = new MultiStrategyFeatureExtractor<string>(new[] { fe0 });

            var vect1 = multiStrategyFeatureExtractor1.ExtractIVector("a");

            Assert.That(multiStrategyFeatureExtractor1.VectorSize, Is.EqualTo(2));
            Assert.That(vect1.Size, Is.EqualTo(2));
            Assert.That(vect1[0], Is.EqualTo(1.67d));
            Assert.That(vect1[1], Is.EqualTo(2.5d));
        }

        [Test]
        public void ExtractIVector_GivenMultipleInnerFeatureExtractor_ReturnsExpectedVector()
        {
            var fe0 = new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 1.67d, 2.5d }, 2);
            var fe1 = new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 3.2d, 6.9d, 8.1d }, 3);

            var multiStrategyFeatureExtractor1 = new MultiStrategyFeatureExtractor<string>(new[] { fe0, fe1 });

            var vect1 = multiStrategyFeatureExtractor1.ExtractIVector("a");

            Assert.That(multiStrategyFeatureExtractor1.VectorSize, Is.EqualTo(5));
            Assert.That(vect1.Size, Is.EqualTo(5));
            Assert.That(vect1[0], Is.EqualTo(1.67d));
            Assert.That(vect1[1], Is.EqualTo(2.5d));
            Assert.That(vect1[2], Is.EqualTo(3.2d));
            Assert.That(vect1[3], Is.EqualTo(6.9d));
            Assert.That(vect1[4], Is.EqualTo(8.1d));
        }

        [Test]
        public void GivenMultipleFeatureExtractors_CanExportAndImportAsVectorDocuments()
        {
            var fe0a = new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 1d }, 1);
            var fe1a = new MultiFunctionFeatureExtractor<string>(new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 2d, 3d }, 2));

            var multiStrategyFeatureExtractor1 = new MultiStrategyFeatureExtractor<string>(new IFloatingPointFeatureExtractor<string>[] { fe0a, fe1a });

            var fe0b = new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 1d }, 1);
            var fe1b = new MultiFunctionFeatureExtractor<string>(new DelegatingFloatingPointFeatureExtractor<string>(s => new[] { 2d, 3d }, 2));

            var multiStrategyFeatureExtractor2 = new MultiStrategyFeatureExtractor<string>(new IFloatingPointFeatureExtractor<string>[] { fe0b, fe1b });

            var transform = new SerialisableVectorTransformation(new[] { new VectorOperation(VectorOperationType.Subtract, new ColumnVector1D(4, 4)) });

            fe1a.PreprocessWith(transform);

            var data = multiStrategyFeatureExtractor1.ToVectorDocument();

            multiStrategyFeatureExtractor2.FromVectorDocument(data);

            var vect1 = multiStrategyFeatureExtractor1.ExtractIVector("a");
            var vect2 = multiStrategyFeatureExtractor2.ExtractIVector("a");

            Assert.That(vect1.Equals(vect2));
        }
    }
}