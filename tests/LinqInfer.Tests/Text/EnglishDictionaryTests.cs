using LinqInfer.Text;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Language
{
    [TestFixture]
    public class EnglishDictionaryTests
    {
        [Test]
        public void FindWordsLike_ReturnsValidResultSet()
        {
            var dict = new EnglishDictionary();

            var like = dict.FindWordsLike("test");

            Assert.That(like.Count > 0);
            Assert.That(like.Values.All(v => v.Value >= 0.75f));
            Assert.That(like.Keys.All(k => "test".ComputeLevenshteinDistance(k) <= 1));
        }

        [Test]
        public void FindWordsLike_IgnoresCase()
        {
            var dict = new EnglishDictionary();

            var like = dict.FindWordsLike("JuMble");

            Assert.That(like.Count > 0);
        }

        [Test]
        public void IsDefined_ReturnsFalseForRandomWord()
        {
            var dict = new EnglishDictionary();

            Assert.That(dict.IsDefined("xomsdf"), Is.False);
        }

        [Test]
        public void Encode_Decode_ReconstructsSameWords_IgnoringCase()
        {
            var dict = new EnglishDictionary();
            var encoded = dict.Encode("apples Pears oranges".Split(' '));
            var decoded = dict.Decode(encoded).ToList();

            Assert.That(decoded[0], Is.EqualTo("apples"));
            Assert.That(decoded[1], Is.EqualTo("pears"));
            Assert.That(decoded[2], Is.EqualTo("oranges"));
        }

        [Test]
        public void Encode_DoNotAppend_ReturnsEnumerationOfIds()
        {
            var words = "big bad xyz wolf".Split(' ');
            var dict = new EnglishDictionary();

            var encoded = dict.Encode(words, false).ToList();

            Assert.That(encoded.Count, Is.EqualTo(4));
            Assert.That(encoded.Distinct().Count(), Is.EqualTo(4));
            Assert.That(encoded[2], Is.EqualTo(0));
            Assert.That(encoded[0], Is.GreaterThan(0));
            Assert.That(encoded[1], Is.GreaterThan(0));
            Assert.That(encoded[3], Is.GreaterThan(0));
        }

        [Test]
        public void Encode_Append_AppendsUnknownWordAndReturnsEnumerationOfIds()
        {
            var words = "big bad xyz wolf".Split(' ');
            var dict = new EnglishDictionary();

            var encoded = dict.Encode(words, true).ToList();

            Assert.That(encoded.Count, Is.EqualTo(4));
            Assert.That(encoded[0], Is.GreaterThan(0));
            Assert.That(encoded[1], Is.GreaterThan(0));
            Assert.That(encoded[2], Is.GreaterThan(0));
            Assert.That(encoded[3], Is.GreaterThan(0));
            Assert.That(dict.IdOf("xyz"), Is.GreaterThan(0));
        }
    }
}
