using LinqInfer.Text;
using LinqInfer.Text.VectorExtraction;
using NUnit.Framework;
using System;
using System.IO;

namespace LinqInfer.Tests.Text.VectorExtraction
{
    [TestFixture]
    public class VectorExtractorTests
    {
        [Test]
        public void ExtractColumnVector_ReturnsExpectedData()
        {
            var ve = new TextVectorExtractor(new[] { "apples", "oranges" }, 5);

            var token1 = new Token("apples", 0);
            var token2 = new Token("jam", 0);

            var vector = ve.ExtractColumnVector(new[] { token1, token2, token1 });

            Assert.That(vector.Size, Is.EqualTo(2));
            Assert.That(vector[1], Is.EqualTo(0f));
            Assert.That((int)(vector[0] * 1000), Is.EqualTo(682));
        }

        [Test]
        public void ExtractColumnVector_SaveLoad_SerialisesAsExpected()
        {
            var ve = new TextVectorExtractor(new[] { "apples", "oranges" }, 5);

            using(var ms = new MemoryStream())
            {
                ve.Save(ms);

                var data = ms.ToArray();

                using (var input = new MemoryStream(data))
                {
                    var ve2 = new TextVectorExtractor();

                    ve2.Load(input);

                    var vector = ve2.ExtractColumnVector(new[] { new Token("oranges", 0) });

                    Assert.That(vector.Size, Is.EqualTo(2));
                    Assert.That((int)(vector[1] * 1000), Is.EqualTo(430));
                }
            }
        }
    }
}
