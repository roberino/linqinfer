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
    }
}
