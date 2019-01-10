using LinqInfer.Learning;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class ClassificationExtensionsTests
    {
        [Test]
        public async Task CreateTimeSequenceTrainingSet_ExportData_CanOpenAsTextClassifier()
        {
            var docs = TestData.TestDocuments();

            var tokenDocs = docs.AsTokenisedDocuments(k => k.Root.Attribute("id").Value);

            var corpus = new Corpus();

            corpus.Append(tokenDocs.SelectMany(d => d.Tokens));

            var semanticSet = await corpus.ExtractKeyTermsAsync(CancellationToken.None);

            var trainingSet = corpus.CreateTimeSequenceTrainingSet(semanticSet);

            var lstm = trainingSet.AttachLongShortTermMemoryNetwork();
            
            var data = lstm.ExportData();

            var classifier = data.OpenTextClassifier();

            Assert.That(classifier, Is.Not.Null);
        }

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
                a = "hi there you",
                b = "?"
            });

            foreach (var result in results.OrderByDescending(r => r.Score))
            {
                Console.WriteLine(result);
            }

            Assert.That(results
                    .OrderByDescending(r => r.Score).First().ClassType,
                Is.EqualTo("greeting"));
        }
    }
}
