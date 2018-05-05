using LinqInfer.Text;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class HtmlEntityMapTests
    {
        [TestCase("apos")]
        [TestCase("quot")]
        [TestCase("gt")]
        [TestCase("lt")]
        [TestCase("amp")]
        public void TryGetXmlEntity_ValidXmlEntity_ReturnsSameEntity(string name)
        {
            var m = new HtmlEntityMap();
            var ent = "&" + name + ";";
            var xml = m.TryGetXmlEntity(ent);

            Assert.That(xml, Is.EqualTo(ent));
        }

        [TestCase("pi", 960)]
        public void TryGetXmlEntity_ValidHtmlEntity(string name, int value)
        {
            var m = new HtmlEntityMap();

            var ent = "&" + name + ";";
            var exp = "&#" + value + ";";

            var xml = m.TryGetXmlEntity(ent);

            Assert.That(xml, Is.EqualTo(exp));
        }

        [TestCase("pi", 960)]
        public void TryDecodeEntityString_ValidHtmlEntity(string name, int value)
        {
            var m = new HtmlEntityMap();

            var ent = "&" + name + ";";
            var exp = "&#" + value + ";";

            var xml = m.TryDecodeEntityString(ent);

            Assert.That(xml, Is.Not.Null);
        }
    }
}
