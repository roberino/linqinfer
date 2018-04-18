using LinqInfer.Learning;
using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class TextExtensionsTests : TestFixtureBase
    {
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

        [Test]
        public void CreateTextFeaturePipeline_ToMLNetwork_SaveRestoresState()
        {
            var data = CreateTestData();

            var pipeline = data.Take(4).AsQueryable().CreateTextFeaturePipeline();

            var classifier = pipeline.AsTrainingSet(x => x.cls).ToMultilayerNetworkClassifier().Execute();

            var state = classifier.ToVectorDocument();

            var classifier2 = state.OpenAsTextualMultilayerNetworkClassifier<string, TestDoc>();
            
            var test = data.Last();

            var r1 = classifier.Classify(test);
            var r2 = classifier2.Classify(test);

            Assert.That(r1.First().ClassType, Is.EqualTo(r2.First().ClassType));
            // Assert.That(Math.Round(r1.First().Score, 2), Is.EqualTo(Math.Round(r2.First().Score, 2)));
        }

        private bool TextFeaturePipelineToNNClassifier()
        {
            var data = CreateTestData();

            var pipeline = data.Take(4).AsQueryable().CreateTextFeaturePipeline();

            var classifier = pipeline.AsTrainingSet(x => x.cls).ToMultilayerNetworkClassifier().Execute();
                        
            var test = data.Last();

            var results = classifier.Classify(test);

            return results.First().ClassType == "B";
        }

        [Test]
        [Category("BuildOmit")]
        public void CreateSemanticClassifier_ReturnsExpectedOutput()
        {
            var data = CreateTestData();

            var classifier = data.Take(4).AsQueryable().CreateSemanticClassifier(x => x.cls, 12);

            var test = data.Last();

            var results = classifier.Classify(test);

            Assert.That(results.First().ClassType, Is.EqualTo("B"));
        }

        private TestDoc[] CreateTestData()
        {
            return new[]
            {
                new TestDoc
                {
                    data = "the time of love and fortune",
                    cls = "G"
                },
                new TestDoc
                {
                    data = "the pain and hate of loss",
                    cls = "B"
                },
                new TestDoc
                {
                    data = "of hurt and sorrow and hell",
                    cls = "B"
                },
                new TestDoc
                {
                    data = "rainbows and sunshine",
                    cls = "G"
                },
                new TestDoc
                {
                    data = "the loss and hell",
                    cls = "?"
                }
            };
        }

        private class TestDoc
        {
            public string data { get; set; }
            public string cls { get; set; }
        }
    }
}