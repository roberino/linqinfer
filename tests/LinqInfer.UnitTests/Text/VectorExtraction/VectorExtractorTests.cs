using System.IO;
using LinqInfer.Text;
using LinqInfer.Text.VectorExtraction;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.VectorExtraction
{
    [TestFixture]
    public class VectorExtractorTests
    {
        [Test]
        public void ExtractColumnVector_ReturnsExpectedData()
        {
            var ve = new TextDataExtractor(new[] { "apples", "oranges" }, 5);

            var token1 = new Token("apples", 0);
            var token2 = new Token("jam", 0);

            var vector = ve.ExtractColumnVector(new[] { token1, token2, token1 });

            Assert.That(vector.Size, Is.EqualTo(6));
            Assert.That(vector[1], Is.EqualTo(0f));
            Assert.That((int)(vector[0] * 1000), Is.EqualTo(806));
        }

        [Test]
        public void ExtractColumnVector_SaveLoad_SerialisesAsExpected()
        {
            var ve = new TextDataExtractor(new[] { "apples", "oranges" }, 5);

            var data = ve.ExportData();

            var ve2 = TextDataExtractor.Create(data);

            var vector = ve2.ExtractColumnVector(new[] { new Token("oranges", 0) });

            Assert.That(vector.Size, Is.EqualTo(6));
            Assert.That((int)(vector[1] * 1000), Is.EqualTo(662));
        }
    }
}