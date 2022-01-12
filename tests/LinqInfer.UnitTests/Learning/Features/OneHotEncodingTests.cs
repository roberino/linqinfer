using LinqInfer.Learning.Features;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class OneHotEncodingTests
    {
        [Test]
        public void Encode_SomeValues_SetsIndexAsExpected()
        {
            var encoder = new OneHotEncoding<string>(10);

            Assert.That(encoder.VectorSize, Is.EqualTo(10));

            foreach (var n in Enumerable.Range(1, 10))
            {
                var vect = encoder.Encode(n.ToString());

                Assert.That(vect.ActiveIndex, Is.EqualTo(n - 1));
                Assert.That(vect.Sum, Is.EqualTo(1));
            }
        }

        [Test]
        public void Encode_ExceedMaxVector_ReturnsZeroForNewValues()
        {
            var encoder = new OneHotEncoding<string>(10);

            foreach (var n in Enumerable.Range(1, 10))
            {
                encoder.Encode(n.ToString());
            }

            var vect = encoder.Encode("a");

            Assert.That(vect.ActiveIndex.HasValue, Is.False);
        }
        
        [Test]
        public void Encode_PredefinedValues_ReturnsExpectedIndex()
        {
            var set = new HashSet<string>(new[] {"a", "b", "c"});

            var encoder = new OneHotEncoding<string>(set);

            Assert.That(encoder.VectorSize, Is.EqualTo(3));
            Assert.That(encoder.Encode("a").ActiveIndex, Is.EqualTo(0));
            Assert.That(encoder.Encode("b").ActiveIndex, Is.EqualTo(1));
            Assert.That(encoder.Encode("c").ActiveIndex, Is.EqualTo(2));
            Assert.That(encoder.Encode("d").ActiveIndex.HasValue, Is.False);
        }

        [Test]
        public void Encode_NonXmlChars_CanExportAndImport()
        {
            var set = new HashSet<string>(10) {"(", "a", "b"};


            var encoder = new OneHotEncoding<string>(set);

            encoder.Encode("(");

            var data = encoder.ExportData().ExportAsXml();

            var data2 = new PortableDataDocument(data);

            var encoder2 = OneHotEncoding<string>.ImportData(data2);

            Assert.That(encoder2.Lookup["("], Is.EqualTo(0));
        }
    }
}