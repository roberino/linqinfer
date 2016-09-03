using LinqInfer.Learning;
using LinqInfer.Text;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class TextExtensionsTests
    {
        [Test]
        public void Tokenise_And_CreateIndex()
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
        public void TermFrequencyIndex_StoreAndRetrieve()
        {
            var docs = new[]
            {
                XDocument.Parse("<doc1>a b c</doc1>"),
                XDocument.Parse("<doc2>a b c d e</doc2>"),
                XDocument.Parse("<doc3>c d e f g</doc3>")
            };

            byte[] indexData;

            using (var ms = new MemoryStream())
            {
                var index = docs.TermFrequencyIndex(d => d.Root.Name.LocalName, ms);

                indexData = ms.ToArray();

                Assert.That(index, Is.Not.Null);

                Assert.That(index("a e").First().Key, Is.EqualTo("doc2"));
            }

            using (var ms = new MemoryStream(indexData))
            {
                var index = ms.OpenAsTermFrequencyIndex();

                Assert.That(index, Is.Not.Null);
                Assert.That(index("a e").First().Key, Is.EqualTo("doc2"));
            }
        }

        [Test]
        public void CreateTextFeaturePipeline_ThenCreateNNClassifier()
        {
            var data = new[]
            {
                new
                {
                    data = "love time fortune",
                    cls = "G"
                },
                new
                {
                    data = "pain hate loss",
                    cls = "B"
                },
                new
                {
                    data = "hurt sorrow hell",
                    cls = "B"
                },
                new
                {
                    data = "rainbows sunshine",
                    cls = "G"
                },
                new
                {
                    data = "loss hell",
                    cls = "?"
                }
            };

            var pipeline = data.Take(4).AsQueryable().CreateTextFeaturePipeline();

            var classifier = pipeline.ToMultilayerNetworkClassifier(x => x.cls).Execute();

            var test = data.Last();

            var results = classifier.Classify(test);

            Assert.That(results.First().ClassType, Is.EqualTo("B"));
        }

        [Test]
        public void CreateSemanticClassifiier_()
        {
            var data = new[]
            {
                new
                {
                    data = "the time of love and fortune",
                    cls = "G"
                },
                new
                {
                    data = "the pain and hate of loss",
                    cls = "B"
                },
                new
                {
                    data = "of hurt and sorrow and hell",
                    cls = "B"
                },
                new
                {
                    data = "rainbows and sunshine",
                    cls = "G"
                },
                new
                {
                    data = "the loss and hell",
                    cls = "?"
                }
            };

            var classifier = data.Take(4).AsQueryable().CreateSemanticClassifiier(x => x.cls, 12);

            var test = data.Last();

            var results = classifier.Classify(test);

            Assert.That(results.First().ClassType, Is.EqualTo("B"));
        }
    }
}