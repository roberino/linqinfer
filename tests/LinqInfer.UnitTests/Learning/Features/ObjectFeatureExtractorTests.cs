using System;
using LinqInfer.Learning.Features;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class ObjectFeatureExtractorTests
    {
        [Test]
        public void CreateFeatureExtractor_ExtractedDataIsCorrect()
        {
            var fe = new ObjectFeatureExtractor<FeatureObject>();

            var data = fe.ExportData();

            var fe2 = ExpressionFeatureExtractor<FeatureObject>.Create(data);

            Assert.That(fe2, Is.Not.Null);
        }

        [Test]
        public void CreateFeatureExtractorFunc_ExtractedDataIsCorrect()
        {
            var fe = new ObjectFeatureExtractor<FeatureObject>();

            var data = fe.ExtractIVector(new FeatureObject()
            {
                Height = 105.23,
                Quantity = 16,
                Width = 123.6f,
                Cost = 12,
                Category = "a",
                Amount = 4323.31M
            });

            Assert.That(data.Size, Is.EqualTo(5));

            Assert.That(data[0], Is.EqualTo(12d));
            Assert.That(data[1], Is.EqualTo(105.23d));
            Assert.That(data[2], Is.EqualTo(16d));
            Assert.That(data[2], Is.EqualTo(16d));
            Assert.That(Math.Round(data[3], 1), Is.EqualTo(123.6d));
            Assert.That(data[4], Is.EqualTo(0.5d));
        }

        class FeatureObject
        {            
            public float Width { get; set; }
            public double Height { get; set; }
            public int Quantity { get; set; }
            public decimal Cost { get; set; }
            [Feature(Ignore = true)]
            public decimal Amount { get; set; }

            [Feature(IndexOrder = 99, Converter = typeof(MyStringConverter), Model = FeatureVectorModel.Categorical)]
            public string Category { get; set; }
        }

        public class MyStringConverter : IValueConverter
        {
            public bool CanConvert(Type type)
            {
                return (type == typeof(string));
            }

            public double Convert(object value)
            {
                return (value as string) == "a" ? 0.5 : 0.1;
            }
        }
    }
}