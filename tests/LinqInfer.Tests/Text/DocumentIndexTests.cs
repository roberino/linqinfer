using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class DocumentIndexTests : TestFixtureBase
    {
        [Test]
        public void ExtractTerms_ReturnsExpectedSet()
        {
            var index = new DocumentIndex();

            var doc1 = new TokenisedTextDocument("1", index.Tokeniser.Tokenise("a a x b"));
            var doc2 = new TokenisedTextDocument("2", index.Tokeniser.Tokenise("c x d"));
            var doc3 = new TokenisedTextDocument("3", index.Tokeniser.Tokenise("e y f 1"));

            index.IndexDocuments(new[] { doc1, doc2, doc3 });

            var setOfTerms = index.ExtractTerms();

            Assert.That(setOfTerms.Count, Is.EqualTo(8));
            Assert.That(setOfTerms.IsDefined("a"));
            Assert.That(setOfTerms.IsDefined("x"));
            Assert.That(setOfTerms.IsDefined("1"), Is.False);
        }

        [Test]
        public void Search_Calculates_Tf_Idf_Correctly()
        {
            var index = new DocumentIndex();

            var doc1 = new TokenisedTextDocument("1", index.Tokeniser.Tokenise("a a x b"));
            var doc2 = new TokenisedTextDocument("2", index.Tokeniser.Tokenise("c x d"));
            var doc3 = new TokenisedTextDocument("3", index.Tokeniser.Tokenise("e y f"));

            index.IndexDocuments(new[] { doc1, doc2, doc3 });

            var result = index.Search("a x").Single();

            var totalNumDocs = 3f;
            var docsContainingX = 2f;
            var docsContainingA = 1f;

            var idf_of_x = Math.Log(totalNumDocs / docsContainingX);
            var idf_of_a = Math.Log(totalNumDocs / docsContainingA);
            var tf_of_x_doc1 = 1f;
            var tf_of_a_doc1 = 2f;

            var tf_idf_x = tf_of_x_doc1 * (idf_of_x + 1);
            var tf_idf_a = tf_of_a_doc1 * (idf_of_a + 1);

            Assert.That(result.Score, IsAround(tf_idf_x * tf_idf_a));
        }

        [Test]
        public void IndexDocument_SingleTerm_SetsFrequencyDataCorrectly()
        {
            var index = new DocumentIndex();

            var doc = new TokenisedTextDocument("1", index.Tokeniser.Tokenise("x"));

            index.IndexDocument(doc);

            var freqData = index.WordFrequencies.Single(f => f.Item1 == "x");

            Assert.That(freqData.Item1, Is.EqualTo("x"));
            Assert.That(freqData.Item2, Is.EqualTo(1));
            Assert.That(freqData.Item3, Is.EqualTo(1));
        }

        [Test]
        public void IndexDocument_SingleTerm_MultipleDocs_SetsFrequencyDataCorrectly()
        {
            var index = new DocumentIndex();

            var doc1 = new TokenisedTextDocument("1", index.Tokeniser.Tokenise("x"));
            var doc2 = new TokenisedTextDocument("2", index.Tokeniser.Tokenise("x x"));

            index.IndexDocuments(new[] { doc1, doc2 });

            var freqData = index.WordFrequenciesByDocumentKey("2").Single();

            Assert.That(freqData.Term, Is.EqualTo("x"));
            Assert.That(freqData.DocumentFrequency, Is.EqualTo(2));
            Assert.That(freqData.TermFrequency, Is.EqualTo(2));
        }

        [Test]
        public void IndexText_ThenSearch_MultipleMatches_ScoresHigher()
        {
            var index = new DocumentIndex();

            index.IndexText("cat sat on mat", "cat-m");
            index.IndexText("cat sat on sofa", "cat-s");
            index.IndexText("dog sat on mat", "dog-m");
            index.IndexText("dog sat on sofa", "dog-s");
            index.IndexText("dog sat on dog", "dog-x");

            var results1 = index.Search("cat sat").ToList();

            Assert.That(results1.Count, Is.EqualTo(2));
            Assert.That(results1.Any(r => r.DocumentKey == "cat-m"));

            var results2 = index.Search("cat mat").ToList();

            Assert.That(results2.Count, Is.EqualTo(1));
            Assert.That(results2.Single().DocumentKey, Is.EqualTo("cat-m"));

            var results3 = index.Search("sat dog").ToList();

            Assert.That(results3.Count, Is.EqualTo(3));
            Assert.That(results3.First().DocumentKey, Is.EqualTo("dog-x"));
        }

        [Test]
        public void Index_Then_Export_ReturnsExpectedXml()
        {
            var search = new DocumentIndex();

            var docs = TestData.TestCorpus().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

            search.IndexDocuments(docs, d => d.Root.Attribute("id").Value);

            var xml = search.ExportAsXml();

            Assert.That(xml.Root.Name.LocalName, Is.EqualTo("index"));
            Assert.That(xml.Root.Attribute("doc-count").Value, Is.EqualTo(docs.Count().ToString()));
            Assert.That(xml.Root.Elements().All(e => e.Name.LocalName == "term"));

            xml.WriteTo(XmlWriter.Create(Console.Out, new XmlWriterSettings() { Indent = true }));
        }

        [Test]
        public void Export_Then_Import_RestoresState()
        {
            var index1 = new DocumentIndex();

            var docs = TestData.TestCorpus().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

            index1.IndexDocuments(docs, d => d.Root.Attribute("id").Value);

            var xml = index1.ExportAsXml();

            var index2 = new DocumentIndex(index1.Tokeniser);

            index2.ImportXml(xml);

            var xml2 = index2.ExportAsXml();

            Assert.That(xml.ToString(), Is.EqualTo(xml2.ToString()));
        }

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