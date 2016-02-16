using LinqInfer.Learning;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void ToSofm_SimpleSample_ClassifiesAsExpected()
        {
            var sample0 = new TestData.Pirate() { Gold = 1800, Age = 70, IsCaptain = true, Ships = 5 };
            var pirateSample = TestData.CreatePirates().ToList();
            var featureMap = pirateSample.AsQueryable().ToSofm(sample0, 2).ToList();

            Assert.That(featureMap.Count(), Is.EqualTo(2));

            var youngPoorPirates = featureMap.Single(m => m.Data.Any(p => p.Key.Age == 25));
            var oldRichPirates = featureMap.Single(m => m.Data.Any(p => p.Key.Age == 64));

            Assert.That(youngPoorPirates.Data.Single(p => p.Key.Age == 21).Value == 1);
            Assert.That(youngPoorPirates.Data.Single(p => p.Key.Age == 19).Value == 1);
            Assert.That(youngPoorPirates.Data.Single(p => p.Key.Age == 18).Value == 1);

            Assert.That(oldRichPirates.Data.Single(p => p.Key.Age == 45).Value == 1);
            Assert.That(oldRichPirates.Data.Single(p => p.Key.Age == 52).Value == 1);
        }

        [Test]
        public void ToSimpleClassifier_SimpleSample_ClassifiesAsExpected()
        {
            var pirateSample = TestData.CreatePirates().ToList();
            var classifier = pirateSample.AsQueryable().ToSimpleClassifier(p => p.Age > 25 ? "old" : "young");

            var classOfPirate = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 120,
                Age = 26,
                IsCaptain = false,
                Ships = 1
            });

            var classOfPirate2 = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 1600,
                Age = 41,
                IsCaptain = true,
                Ships = 4
            });

            Assert.That(classOfPirate.ClassType, Is.EqualTo("young"));
            Assert.That(classOfPirate2.ClassType, Is.EqualTo("old"));
        }
    }
}
