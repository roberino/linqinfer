using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class MultiStrategyFeatureExtractorTests
    {
        [Test]
        public void ExtractIVector_GivenSingleInnerFeatureExtractor_ReturnsExpectedVector()
        {
            var fe0 = new ExpressionFeatureExtractor<string>(s => ColumnVector1D.Create(1.67d, 2.5d), 2);

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
            var fe0 = new ExpressionFeatureExtractor<string>(s => ColumnVector1D.Create(1.67d, 2.5d), 2);
            var fe1 = new ExpressionFeatureExtractor<string>(s => ColumnVector1D.Create(3.2d, 6.9d, 8.1d), 3);

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
            var fe0A = new ExpressionFeatureExtractor<string>(s => ColumnVector1D.Create( 1d ), 1);
            var fe1A = new TransformingFeatureExtractor<string>(new ExpressionFeatureExtractor<string>(s => ColumnVector1D.Create( 2d, 3d), 2));

            var multiStrategyFeatureExtractor1 = new MultiStrategyFeatureExtractor<string>(fe0A, fe1A);
            
            var transform = new SerialisableDataTransformation(new DataOperation(VectorOperationType.Subtract, new ColumnVector1D(4, 4)));

            fe1A.AddTransform(transform);

            var data = multiStrategyFeatureExtractor1.ExportData();

            var multiStrategyFeatureExtractor2 = MultiStrategyFeatureExtractor<string>.Create(data);

            var vect1 = multiStrategyFeatureExtractor1.ExtractIVector("a");
            var vect2 = multiStrategyFeatureExtractor2.ExtractIVector("a");

            Assert.That(vect1.Equals(vect2));
        }
    }
}