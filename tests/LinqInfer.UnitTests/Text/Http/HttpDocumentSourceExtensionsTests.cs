using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;
using LinqInfer.Text;
using LinqInfer.Text.Http;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.Http
{
    [TestFixture]
    public class HttpDocumentSourceExtensionsTests
    {
        private readonly Uri _defaultUrl = new Uri("http://x/");

        private IAsyncEnumerator<HttpDocument> CreateDocumentSource()
        {
            var doc = new HttpDocument(_defaultUrl, "big bad wolf".Tokenise());

            var source = From.Enumerable(new[]
            {
                doc
            });

            return source;
        }

        [Test]
        public async Task AttachIndex_WhenGivenSingleDoc_ThenCreatesIndexWhenPipeRun()
        {
            var source = CreateDocumentSource();

            var pipe = source.CreatePipe();
            var index = pipe.AttachIndex();

            Assert.That(index.DocumentCount, Is.EqualTo(0));

            await pipe.RunAsync(CancellationToken.None);

            Assert.That(index.DocumentCount, Is.EqualTo(1));
            Assert.That(index.Search("big").Single().DocumentKey, Is.EqualTo(_defaultUrl.ToString()));
            Assert.That(index.Search("bad").Single().DocumentKey, Is.EqualTo(_defaultUrl.ToString()));
            Assert.That(index.Search("wolf").Single().DocumentKey, Is.EqualTo(_defaultUrl.ToString()));
        }

        [Test]
        public async Task AttachCorpus_WhenGivenSingleDoc_ThenCreatesCorpusWhenPipeRun()
        {
            var source = CreateDocumentSource();

            var pipe = source.CreatePipe();
            var corpus = pipe.AttachCorpus();

            Assert.That(corpus.Blocks.Count(), Is.EqualTo(0));

            await pipe.RunAsync(CancellationToken.None);

            Assert.That(corpus.Blocks.Any());
        }
    }
}
