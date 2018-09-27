using System.Linq;
using LinqInfer.Text;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class TextUtilityTests
    {
        [Test]
        public void AsBytes_ReturnsValidByteArray()
        {
            var bytes = "abc".AsBytes(TextEncoding.Ascii);

            Assert.That(bytes.Length == 3);

            Assert.That(bytes[0], Is.EqualTo((byte)'a'));
            Assert.That(bytes[1], Is.EqualTo((byte)'b'));
            Assert.That(bytes[2], Is.EqualTo((byte)'c'));
        }

        [Test]
        public void AsBytes_ThenAsString_ReconstructsString()
        {
            var bytes = "abc & yo".AsBytes();
            var str = bytes.AsString();

            Assert.That(str, Is.EqualTo("abc & yo"));
        }

        [Test]
        public void AsReader_AsTokenisedLines_ReturnsEnumerationOfTokenisedLines()
        {
            var lines = "abc\ndef 123".AsReader().AsTokenisedLines().ToList();

            Assert.That(lines.Count, Is.EqualTo(2));
            Assert.That(lines[0][0].Text, Is.EqualTo("abc"));
            Assert.That(lines[1][0].Text, Is.EqualTo("def"));
            Assert.That(lines[1][2].Text, Is.EqualTo("123"));
        }
    }
}