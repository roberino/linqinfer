using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.UnitTests.Text
{
    [TestFixture]
    public class ClassificationExtensionsTests
    {
        public void T()
        {
            var corpus = TestData.TestCorpus();
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
