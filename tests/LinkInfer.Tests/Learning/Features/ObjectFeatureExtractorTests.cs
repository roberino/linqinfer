using LinqInfer.Learning.Features;
using NUnit.Framework;

namespace LinqInfer.Tests.Learning.Features
{
    [TestFixture]
    public class ObjectFeatureExtractorTests
    {
        [Test]
        public void CreateFeatureExtractor_SimpleObject()
        {
            var fo = new ObjectFeatureExtractor();
            var fe = fo.CreateFeatureExtractorFunc<FeatureObject>();

            var data = fe(new FeatureObject()
            {
                Height = 105.23,
                Quantity = 16,
                Width = 123.6f,
                Cost = 12
            });

            Assert.That(data[0], Is.EqualTo(12f));
            Assert.That(data[1], Is.EqualTo(105.23f));
            Assert.That(data[2], Is.EqualTo(16f));
            Assert.That(data[3], Is.EqualTo(123.6f));
        }

        private class FeatureObject
        {
            public float Width { get; set; }
            public double Height { get; set; }
            public int Quantity { get; set; }
            public decimal Cost { get; set; }
        }
    }
}
