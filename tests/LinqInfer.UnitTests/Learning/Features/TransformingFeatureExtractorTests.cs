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
            var ext = new ObjectFeatureExtractorFactory();
            var fe = ext.CreateFeatureExtractor<FeatureObject>();
            var tfe = new TransformingFeatureExtractor<FeatureObject, double>(fe, v => new double[] { v[0] * v[1] * v[2] });

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

        [Test]
        public void FeatureMetadata_FilterOnlyX_ExpectSingleItem()
        {
            var ext = new ObjectFeatureExtractorFactory();
            var fe = ext.CreateFeatureExtractor<FeatureObject>();
            var tfe = new TransformingFeatureExtractor<FeatureObject, double>(fe, null, f => f.Label == "x");

            var featurex = tfe.FeatureMetadata.Single();

            Assert.That(featurex.DataType == System.TypeCode.Int32);
            Assert.That(featurex.Label == "x");
        }

        private class FeatureObject
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
    }
}
