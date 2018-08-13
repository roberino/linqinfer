using System.Linq;
using LinqInfer.Learning.Features;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class TransformingFeatureExtractorTests
    {
        [Test]
        public void FeatureMetadata_AggregateInputsInto1D_ExpectSingleItem()
        {
            var fe = new ObjectFeatureExtractor<FeatureObject>();
            var tfe = new TransformingFeatureExtractor<FeatureObject>(fe);

            var transformedFeature = tfe.FeatureMetadata.Single();

            Assert.That(transformedFeature.DataType == System.TypeCode.Double);
            Assert.That(transformedFeature.Index == 0);
            Assert.That(transformedFeature.Label == "Transform 1");

            var vector = tfe.ExtractVector(new FeatureObject()
            {
                x = 3,
                y = 5,
                z = 7
            });

            Assert.That(vector.First(), Is.EqualTo(3d * 5d * 7d));
        }

        class FeatureObject
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
    }
}