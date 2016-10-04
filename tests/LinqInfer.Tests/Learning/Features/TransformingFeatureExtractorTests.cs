using LinqInfer.Learning.Features;
using NUnit.Framework;
using System.Linq;

namespace LinqInfer.Tests.Learning.Features
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

            tfe.NormaliseUsing(new FeatureObject[] { new FeatureObject() { x = 10, y = 10, z = 10 } });

            var trasnformedFeature = tfe.FeatureMetadata.Single();

            Assert.That(trasnformedFeature.DataType == System.TypeCode.Double);
            Assert.That(trasnformedFeature.Index == 0);
            Assert.That(trasnformedFeature.Label == "Transform 1");

            var vector = tfe.ExtractVector(new FeatureObject()
            {
                x = 3,
                y = 5,
                z = 7
            });

            Assert.That(vector.First(), Is.EqualTo((3d / 10d) * (5d / 10d) * (7d / 10d)));
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
