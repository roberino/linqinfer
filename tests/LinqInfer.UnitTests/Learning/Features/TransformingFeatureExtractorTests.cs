using System.Linq;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
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

            var tf = new SerialisableDataTransformation(new DataOperation(VectorOperationType.EuclideanDistance,
                new Vector(1, 2, 3)));

            tfe.AddTransform(tf);
            
            var transformedFeature = tfe.FeatureMetadata.Single();

            Assert.That(transformedFeature.DataType == System.TypeCode.Double);
            Assert.That(transformedFeature.Index == 0);
            Assert.That(transformedFeature.Label == "Transform 1");

            var vector = tfe.ExtractIVector(new FeatureObject()
            {
                x = 3,
                y = 5,
                z = 7
            });

            Assert.That(vector.Size, Is.EqualTo(1));
            Assert.That(vector[0], Is.EqualTo(tf.Apply(new Vector(3, 5, 7))[0]));
        }

        class FeatureObject
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
    }
}