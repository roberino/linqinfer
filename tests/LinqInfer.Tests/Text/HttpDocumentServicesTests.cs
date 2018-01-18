using LinqInfer.Maths.Graphs;
using LinqInfer.Text.Analysis;
using LinqInfer.Text.Http;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [Ignore("Integration only")]
    [TestFixture]
    public class HttpDocumentServicesTests
    {
        [TestCase("https://en.wikipedia.org/wiki/Main_Page")]
        public async Task GetDocument_TestUrl_ReturnsHttpDoc(string url)
        {
            using (var reader = new HttpDocumentServices())
            {
                var uri = new Uri(url);
                var doc = await reader.GetDocumentAsync(uri);

                Assert.That(doc.BaseUrl, Is.EqualTo(uri));
                Assert.That(doc.Links.Any());
            }
        }

        [TestCase("https://en.wikipedia.org/wiki/Main_Page", "science")]
        [TestCase("https://en.wikipedia.org/wiki/Portal:Mathematics", "mathematics")]
        [TestCase("https://en.wikipedia.org/wiki/Main_Page", "people")]
        public async Task CreateCorpus_TestUrl_ReturnsCorpus(string url, string word)
        {
            using (var reader = new HttpDocumentServices())
            {
                var uri = new Uri(url);
                var corpus = await reader.CreateDocumentSource(uri).CreateCorpusAsync();

                var graph = await corpus.ExportWordGraph(word);

                var gexf = await graph.ExportAsGexfAsync();

                // gexf.Save($"c:\\git\\wiki-{word}.gexf");
            }
        }

        [TestCase("https://en.wikipedia.org/wiki/Portal:Mathematics", "mathematics;science;philosophy")]
        public async Task CreateCorpus2_TestUrl_ReturnsCorpus(string url, string word)
        {
            using (var reader = new HttpDocumentServices())
            {
                var uri = new Uri(url);
                var corpus = await reader.CreateDocumentSource(uri).CreateCorpusAsync();

                var graph = await corpus.ExportWordGraph(t => word.Contains(t.Text.ToLower()));

                var science = await graph.FindVertexAsync("science");
                var mathematics = await graph.FindVertexAsync("mathematics");

                var cosineSim = await science.VertexCosineSimilarityAsync(mathematics);

                Console.WriteLine(cosineSim);

                var gexf = await graph.ExportAsGexfAsync();

                // gexf.Save($"c:\\git\\wiki-multi.gexf");
            }
        }
    }
}