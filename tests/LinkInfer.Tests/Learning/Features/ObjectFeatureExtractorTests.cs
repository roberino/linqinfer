using LinqInfer.Learning.Features;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;

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
                Cost = 12,
                Category = "a",
                Amount = 4323.31M
            });

            Assert.That(data.Length, Is.EqualTo(5));

            Assert.That(data[0], Is.EqualTo(12d));
            Assert.That(data[1], Is.EqualTo(105.23d));
            Assert.That(data[2], Is.EqualTo(16d));
            Assert.That(data[2], Is.EqualTo(16d));
            Assert.That(Math.Round(data[3], 1), Is.EqualTo(123.6d));
            Assert.That(data[4], Is.EqualTo(0.5d));
        }

        private class FeatureObject
        {            
            public float Width { get; set; }
            public double Height { get; set; }
            public int Quantity { get; set; }
            public decimal Cost { get; set; }
            [Feature(Ignore = true)]
            public decimal Amount { get; set; }

            [Feature(IndexOrder = 99, Converter = typeof(MyStringConverter), Model = DistributionModel.Categorical)]
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
