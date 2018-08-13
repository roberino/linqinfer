using System.Linq;
using LinqInfer.Learning.Features;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
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
        public void NormaliseAndExtractVectors_ReturnsValidData()
        {
            var data = TestData.CreateQueryablePirates();

            var minVal = data.Min(x => x.Age);
            var maxVal = data.Max(x => x.Age);
            var expectedVal = ((double)data.First().Age - minVal) / (maxVal - minVal);

            var pipeline = new FeatureProcessingPipeline<TestData.Pirate>(data).NormaliseData();

            var featureData = pipeline.ExtractVectors().ToList();

            Assert.That(featureData.Count, Is.EqualTo(data.Count()));

            Assert.That(featureData[0][0], Is.EqualTo(expectedVal));
        }
    }
}
