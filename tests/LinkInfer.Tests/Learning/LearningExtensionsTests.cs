using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class LearningExtensionsTests
    {
        [Test]
        public void ToSofm_SimpleSample_ClassifiesAsExpected()
        {
            var sample0 = new TestData.Pirate() { Gold = 1800, Age = 70, IsCaptain = true, Ships = 5 };
            var pirateSample = TestData.CreatePirates().ToList();
            var featureMap = pirateSample.AsQueryable().ToSofm(sample0, 2).ToList();

            Assert.That(featureMap.Count(), Is.EqualTo(2));

            var youngPoorPirates = featureMap.Single(m => m.GetMembers().Any(p => p.Key.Age == 25));
            var oldRichPirates = featureMap.Single(m => m.GetMembers().Any(p => p.Key.Age == 64));

            Assert.That(youngPoorPirates.GetMembers().Single(p => p.Key.Age == 21).Value == 1);
            Assert.That(youngPoorPirates.GetMembers().Single(p => p.Key.Age == 19).Value == 1);
            Assert.That(youngPoorPirates.GetMembers().Single(p => p.Key.Age == 18).Value == 1);

            Assert.That(oldRichPirates.GetMembers().Single(p => p.Key.Age == 45).Value == 1);
            Assert.That(oldRichPirates.GetMembers().Single(p => p.Key.Age == 52).Value == 1);
        }

        [Test]
        public void ToNaiveBayesClassifier_SimpleSample_ClassifiesAsExpected()
        {
            var pirateSample = TestData.CreatePirates().ToList();
            var classifier = pirateSample.AsQueryable().ToNaiveBayesClassifier(p => p.Age > 25 ? "old" : "young");

            // In the original predicate, if age > 25 then old.
            // But this pirate shares many features of other young pirates
            // So therfore should be classed as "young"
            var classOfPirate = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 120,
                Age = 27,
                IsCaptain = false,
                Ships = 1
            }).FirstOrDefault();

            var classOfPirate2 = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 1600,
                Age = 41,
                IsCaptain = true,
                Ships = 4
            }).FirstOrDefault();

            Assert.That(classOfPirate.ClassType, Is.EqualTo("young"));
            Assert.That(classOfPirate2.ClassType, Is.EqualTo("old"));
        }

        [Test]
        public void ToMultilayerNetworkClassifier_XorSample_ClassifiesAsExpected()
        {
            var xor1 = new XorNode() { X = true, Y = false };
            var xor2 = new XorNode() { X = false, Y = false };
            var xor3 = new XorNode() { X = true, Y = true };
            var xor4 = new XorNode() { X = false, Y = true };

            var samples = new[] { xor1, xor2, xor3, xor4 };

            var classifier = samples.AsQueryable().ToMultilayerNetworkClassifier(x => x.Output, f =>
            {
                return
                    f(xor1).ClassType == xor1.Output
                    && f(xor2).ClassType == xor2.Output
                    && f(xor3).ClassType == xor3.Output
                    && f(xor4).ClassType == xor4.Output;
            }, 0.3f);

            var classResults1 = classifier.Invoke(xor1).First();
            var classResults2 = classifier.Invoke(xor2).First();
            var classResults3 = classifier.Invoke(xor3).First();
            var classResults4 = classifier.Invoke(xor4).First();

            Assert.That(classResults1.ClassType == xor1.Output);
            Assert.That(classResults2.ClassType == xor2.Output);
            Assert.That(classResults3.ClassType == xor3.Output);
            Assert.That(classResults4.ClassType == xor4.Output);
        }

        [Test]
        public void ToMultilayerNetworkClassifier_SimpleSample_ClassifiesAsExpected()
        {
            int successCounter = 0; int failureCounter = 0;

            foreach (var i in Enumerable.Range(1, 25))
            {
                var pirateSample = TestData.CreatePirates().ToList();
                var classifier = pirateSample.AsQueryable().ToMultilayerNetworkClassifier(p => p.Age > 25 ? "old" : "young", 0.1f);

                // In the original predicate, if age > 25 then old.
                // But this pirate shares many features of other young pirates
                // So therfore should be classed as "young"
                var classOfPirate = classifier.Invoke(new TestData.Pirate()
                {
                    Gold = 120,
                    Age = 5,
                    IsCaptain = false,
                    Ships = 1
                }).FirstOrDefault();

                var classOfPirate2 = classifier.Invoke(new TestData.Pirate()
                {
                    Gold = 1600,
                    Age = 61,
                    IsCaptain = true,
                    Ships = 7
                }).FirstOrDefault();

                try
                {
                    Assert.That(classOfPirate.ClassType, Is.EqualTo("young"));
                    Assert.That(classOfPirate2.ClassType, Is.EqualTo("old"));
                    Console.WriteLine("=> SUCCESS");
                    successCounter++;
                }
                catch
                {
                    Console.WriteLine("=> FAILURE");
                    failureCounter++;
                }
            }

            Console.WriteLine("successes:{0},failures:{1}", successCounter, failureCounter);
            Assert.That((float)successCounter / (float)failureCounter, Is.GreaterThan(3f));
        }

        [Test]
        public void ToSimpleClassDistribution_SimpleSample()
        {
            var pirateSample = TestData.CreatePirates().ToList();
            var classifier = pirateSample.AsQueryable().ToSimpleDistributionFunction(p => p.Age > 25 ? "old" : "young");

            var distribution = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 70,
                Age = 26,
                IsCaptain = false,
                Ships = 1
            });

            var total = distribution.Values.Sum();

            Assert.That(total.Value, Is.EqualTo(1));
            Assert.That(distribution.Values.All(v => v.Value >= 0f));
        }

        [Test]
        public void ToSimpleClassDistribution_SimpleSample_()
        {
            var pirateSample = TestData.CreatePirates().ToList();

            var kde = new KernelDensityEstimator();
            var fo = new ObjectFeatureExtractor();
            var fe = fo.CreateFeatureExtractorFunc<TestData.Pirate>();
            var dist = kde.Evaluate(pirateSample.Select(s => new ColumnVector1D(fe(s))).AsQueryable());

            var classifier = pirateSample.AsQueryable().ToSimpleDistributionFunction(p => p.Age > 25 ? "old" : "young");

            var distribution = classifier.Invoke(new TestData.Pirate()
            {
                Gold = 70,
                Age = 26,
                IsCaptain = false,
                Ships = 1
            });

            var total = distribution.Values.Sum();

            Assert.That(total.Value, Is.EqualTo(1));
            Assert.That(distribution.Values.All(v => v.Value >= 0f));
        }
    }
}
