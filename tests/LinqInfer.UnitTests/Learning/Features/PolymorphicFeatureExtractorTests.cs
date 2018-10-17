using LinqInfer.Learning.Features;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class PolymorphicFeatureExtractorTests
    {
        [Test]
        public void ExtractIVector_BaseClassAndInherittedClass_ReturnsCorrectSizeAndData()
        {
            var pfe = new PolymorphicFeatureExtractor<FeatureClass1>(8);

            var v1 = pfe.ExtractIVector(new FeatureClass1()
            {
                X = 12
            });
            
            var v2 = pfe.ExtractIVector(new FeatureClass2()
            {
                X = 2,
                Y = 6
            });

            Assert.That(v1.Size, Is.EqualTo(8));
            Assert.That(v2.Size, Is.EqualTo(8));
            
            Assert.That(v1[0], Is.EqualTo(12));
            Assert.That(v1[1], Is.EqualTo(0));

            Assert.That(v2[0], Is.EqualTo(2));
            Assert.That(v2[1], Is.EqualTo(6));


        }

        public class FeatureClass1
        {
            public int X {get;set;}
        }

        public class FeatureClass2 : FeatureClass1
        {
            public int Y {get;set;}
        }

        public class FeatureClass3 : FeatureClass2
        {
            public int Z {get;set;}
        }
    }
}
