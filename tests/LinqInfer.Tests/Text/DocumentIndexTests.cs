using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class DocumentIndexTests : TestFixtureBase
    {
        [Test]
        public void Index_Then_Search()
        {
            var search = new DocumentIndex();

            var docs = TestData.TestCorpus().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

            search.IndexDocuments(docs, d => d.Root.Attribute("id").Value);

            var matches = search.SearchInternal("love time");

            Assert.That(matches.All(m => m.Value > 0));
            Assert.That(matches.First().Key, Is.EqualTo("3"));
        }

        [Test]
        public void CreateVectorExtractor()
        {
            var index = new DocumentIndex();

            var docs = TestData.TestCorpus().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

            int id = 0;

            var blocks = docs.SelectMany(d => new Corpus(index.Tokeniser.Tokenise(d.Root.Value)).Blocks.ToList()).ToList();

            var tdocs = blocks
                .Select(b => new TokenisedTextDocument((id++).ToString(), b))
                .ToList();

            index.IndexDocuments(tdocs);

            var ve = index.CreateVectorExtractor();

            var vect = ve.ExtractColumnVector(index.Tokeniser.Tokenise("love time fortune"));

            Console.WriteLine(vect);
        }

        [Test]
        public void CreateVectorExtractor_LargeCorpus()
        {
            var index = new DocumentIndex();

            using (var corpusStream = GetResource("shakespeare.txt"))
            {
                var corpus = new Corpus(corpusStream.Tokenise());

                int id = 0;

                index.IndexDocuments(corpus.Blocks.Select(b => new TokenisedTextDocument((id++).ToString(), b)));
            }

            var ve = index.CreateVectorExtractor(1024);

            var vect = ve.ExtractColumnVector(index.Tokeniser.Tokenise("love time fortune"));

            Console.WriteLine(vect);
        }
    }
}