using LinqInfer.Text;
using LinqInfer.Text.Http;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class HttpDocumentServicesTests
    {
        //[Ignore("Integration only")]
        [TestCase("https://en.wikipedia.org/wiki/Main_Page")]
        public async Task GetDocument_TestUrl_ReturnsHttpDoc(string url)
        {
            using (var reader = new HttpDocumentServices())
            {
                var uri = new Uri(url);
                var doc = await reader.GetDocument(uri);

                Assert.That(doc.BaseUrl, Is.EqualTo(uri));
                Assert.That(doc.Links.Any());
            }
        }

        //[Ignore("Integration only")]
        [TestCase("https://en.wikipedia.org/wiki/Main_Page", "science")]
        [TestCase("https://en.wikipedia.org/wiki/Main_Page", "math")]
        [TestCase("https://en.wikipedia.org/wiki/Main_Page", "people")]
        public async Task CreateCorpus_TestUrl_ReturnsCorpus(string url, string word)
        {
            using (var reader = new HttpDocumentServices())
            {
                var uri = new Uri(url);
                var corpus = await reader.CreateCorpus(uri, null, 25);

                var graph = await corpus.ExportWordGraph(word);

                var gexf = await graph.ExportAsGexfAsync();

                gexf.Save($"c:\\git\\wiki-{word}.gexf");
            }
        }
    }
}
