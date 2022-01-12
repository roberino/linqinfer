using LinqInfer.Text;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class HtmlExtensionsTests : TestFixtureBase
    {
        [TestCase("html_sample1.html")]
        [TestCase("html_sample2.html")]
        public void OpenAsHtmlDocument_WhenGivenStreamOfHtml_ThenValidDocCreated(string fileName)
        {
            using (var htmlStream = GetResource(fileName))
            {
                var doc = htmlStream.OpenAsHtmlDocument();

                Assert.That(doc.Root.Name.LocalName, Is.EqualTo("html"));
                Assert.That(doc.Root.Element("body").Name.LocalName, Is.EqualTo("body"));
                Assert.That(doc.Root.Element("body").Value.Trim(), Is.EqualTo("Test"));
            }
        }
    }
}