using LinqInfer.Learning;
using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class TextExtensionsTests
    {
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
    }
}