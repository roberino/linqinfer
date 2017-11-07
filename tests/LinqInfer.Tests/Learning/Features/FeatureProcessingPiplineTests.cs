using LinqInfer.Learning.Features;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class FeatureProcessingPiplineTests
    {
        [Test]
        public void CreateInstance_AssertBasicState()
        {
            var data = TestData.CreateQueryablePirates();

            var pipeline = new FeatureProcessingPipeline<TestData.Pirate>(data);

            Assert.That(pipeline.Data, Is.SameAs(data));
            Assert.That(pipeline.FeatureExtractor, Is.Not.Null);
        }

        [Test]
        public void CreateInstance_ExtractVectors_ReturnsValidData()
        {
            var data = TestData.CreateQueryablePirates();

            var pipeline = new FeatureProcessingPipeline<TestData.Pirate>(data);

            var featureData = pipeline.ExtractColumnVectors().ToList();

            Assert.That(featureData.Count, Is.EqualTo(data.Count()));

            Assert.That(featureData[0][0], Is.EqualTo((double)data.First().Age / data.Max(x => x.Age)));
        }

        [Test]
        public void CreateInstance_FilterFeaturesByProperty_ExtractVectors_ReturnsValidData()
        {
            var data = TestData.CreateQueryablePirates();

            var pipeline = new FeatureProcessingPipeline<TestData.Pirate>(data)
                .FilterFeaturesByProperty(p => p
                    .Select(x => x.Age)
                    .Select(x => x.Gold));

            Assert.That(pipeline.FeatureExtractor.FeatureMetadata.Count(), Is.EqualTo(2));
            Assert.That(pipeline.FeatureExtractor.FeatureMetadata.Any(f => f.Label == "Age"));
            Assert.That(pipeline.FeatureExtractor.FeatureMetadata.Any(f => f.Label == "Gold"));
        }

        [Test]
        public void CreateInstance_FilterFeaturesAndPreprocess_ExtractVectors_ReturnsValidData()
        {
            var data = TestData.CreateQueryablePirates();

            var pipeline = new FeatureProcessingPipeline<TestData.Pirate>(data)
                .FilterFeatures(x => x.Label == "Age")
                .PreprocessWith(m =>
                {
                    return new double[] { m.Sum() * 7 };
                });

            var featureData = pipeline.ExtractColumnVectors().ToList();

            Assert.That(featureData.Count, Is.EqualTo(data.Count()));

            Assert.That(featureData[0].Single(), Is.EqualTo((double)data.First().Age / data.Max(x => x.Age) * 7));
        }
    }
}
