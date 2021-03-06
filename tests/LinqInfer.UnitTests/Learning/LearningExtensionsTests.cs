﻿using System;
using System.Linq;
using LinqInfer.Learning;
using LinqInfer.Maths;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class LearningExtensionsTests
    {
        [Test]
        public void ToSofm_SimpleSample_ClassifiesAsExpected()
        {
            var pirateSample = TestData.CreatePirates().ToList();

            var pipeline = pirateSample
                .AsQueryable()
                .CreatePipeline();

            Console.WriteLine($"VectorSize:{pipeline.VectorSize}");

            pipeline
                .CentreFeatures()
                .ScaleFeatures(new Range(1, -1));

            var featureMap = pipeline.ToSofm(2, 0.5f, null, 100).Execute().ToList();

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
    }
}
