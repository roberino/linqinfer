using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class GraphExtensionsTests
    {
        [Test]
        public async Task WhenGivenCorpus_ThenCanExportWordGraph()
        {
            var corpus = CorpusDataSource.GetCorpus();

            var graph = await corpus.ExportWordGraphAsync("life", CancellationToken.None);

            var gexf = await graph.ExportAsGexfAsync();

            Console.WriteLine(gexf);

            TestFixtureBase.SaveArtifact("life.gexf", s => gexf.Save(s));
        }
    }
}