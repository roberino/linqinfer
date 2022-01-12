using LinqInfer.Text;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LinqInfer.Data.Pipes;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class TokeniserExtensionsTests : TestFixtureBase
    {
        [Test]
        public async Task CreateCorpus_FromTextReader_CanReadTokens()
        {
            var corpus = TestData.CreateReader().CreateCorpus();

            var data = await corpus.ReadBlocksAsync().ToMemoryAsync(CancellationToken.None);

            Assert.That(data.Count, Is.GreaterThanOrEqualTo(500));
        }

        [Test]
        public void WhenTokenisedAndIndexed_ThenDocsCanBeSearched()
        {
            var docs = new[]
            {
                XDocument.Parse("<doc1>a b c</doc1>"),
                XDocument.Parse("<doc2>a b c d e</doc2>"),
                XDocument.Parse("<doc3>c d e f g</doc3>")
            };

            var index = docs
                .AsTokenisedDocuments(d => d.Root.Name.LocalName)
                .CreateIndex();

            var results = index.Search("g");
            
            Assert.That(results.Single().DocumentKey == "doc3");
        }

        [Test]
        public void WhenExportedAsXmlAndOpenAsIndex_ThenNewIndexInstanceCanBeCreated()
        {
            var docs = new[]
            {
                XDocument.Parse("<doc1>a b c</doc1>"),
                XDocument.Parse("<doc2>a b c d e</doc2>"),
                XDocument.Parse("<doc3>c d e f g</doc3>")
            };

            var index = docs.AsTokenisedDocuments(k => k.Root.Name.LocalName).CreateIndex();
            var xml = index.ExportAsXml();
            var index2 = xml.OpenAsIndex();

            Assert.That(xml.ToString(), Is.EqualTo(index2.ExportAsXml().ToString()));
        }
    }
}