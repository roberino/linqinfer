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
            var ext = new ObjectFeatureExtractor();
            var fe = ext.CreateFeatureExtractor<FeatureObject>();
            var tfe = new TransformingFeatureExtractor<FeatureObject, double>(fe, v => new double[] { v[0] * v[1] * v[2] });

            tfe.FeatureMetadata.Single();
        }

        private class FeatureObject
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
    }
}
