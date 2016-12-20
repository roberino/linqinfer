﻿using LinqInfer.Learning;
using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class TextExtensionsTests : TestFixtureBase
    {
        [Test]
        public void OpenAsHtmlDocument_ReturnsValidDoc()
        {
            using (var htmlStream = GetResource("html_sample1.html"))
            {
                var doc = htmlStream.OpenAsHtmlDocument();

                Assert.That(doc.Root.Name.LocalName, Is.EqualTo("html"));
                Assert.That(doc.Root.Elements().Single().Name.LocalName, Is.EqualTo("body"));
                Assert.That(doc.Root.Elements().Single().Value.Trim(), Is.EqualTo("Test"));
            }
        }

        [Test]
        public void OpenAsHtmlDocument2_ReturnsValidDoc()
        {
            using (var htmlStream = GetResource("html_sample2.html"))
            {
                var doc = htmlStream.OpenAsHtmlDocument();

                Assert.That(doc.Root.Name.LocalName, Is.EqualTo("html"));

                var rootElements = doc.Root.Elements().ToList();

                Assert.That(rootElements.Skip(1).First().Name.LocalName, Is.EqualTo("body"));
            }
        }

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
        public void ExportAsXml_ThenOpenAsIndex()
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

        [TestCase(75, 5)]
        public void CreateTextFeaturePipeline_ThenCreateNNClassifier(float passPercent, int iterations)
        {
            double t = 0;

            foreach (var x in Enumerable.Range(0, iterations))
            {
                if (TextFeaturePipelineToNNClassifier())
                {
                    t += 1;
                }
            }

            Console.WriteLine("{0:P} correct", t / (float)iterations);

            Assert.That(t, Is.GreaterThanOrEqualTo(passPercent / 100f));
        }

        public bool TextFeaturePipelineToNNClassifier()
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

            return results.First().ClassType == "B";
        }

        [Test]
        public void CreateSemanticClassifier_ReturnsExpectedOutput()
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

            var classifier = data.Take(4).AsQueryable().CreateSemanticClassifier(x => x.cls, 12);

            var test = data.Last();

            var results = classifier.Classify(test);

            Assert.That(results.First().ClassType, Is.EqualTo("B"));
        }
    }
}