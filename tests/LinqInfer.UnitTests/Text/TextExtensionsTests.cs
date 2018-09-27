using System;
using System.Linq;
using System.Xml.Linq;
using LinqInfer.Text;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class TextExtensionsTests : TestFixtureBase
    {
        [Test]
        public void CreateSemanticClassifier_WithAnonymousObjects_ReturnsClassifier()
        {
            var classifier = new[]
                {
                    new
                    {
                        a = "hey there man",
                        b = "greeting"
                    },
                    new
                    {
                        a = "x y z",
                        b = "other"
                    },
                    new
                    {
                        a = "hi there man",
                        b = "greeting"
                    },
                    new
                    {
                        a = "z z z",
                        b = "other"
                    }
                }
                .AsQueryable()
                .CreateSemanticClassifier(x => x.b);

            var results = classifier.Classify(new
            {
                a = "hi you",
                b = "?"
            });

            foreach (var result in results.OrderByDescending(r => r.Score))
            {
                Console.WriteLine(result);
            }

            Assert.That(results.OrderByDescending(r => r.Score).First().ClassType, 
                Is.EqualTo("greeting"));
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