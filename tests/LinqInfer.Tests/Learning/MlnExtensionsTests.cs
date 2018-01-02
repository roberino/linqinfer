﻿using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class MlnExtensionsTests
    {
        [Test]
        public async Task AttachMultilayerNetworkClassifier_WhenTrainingSetRun_ReturnsClassifier()
        {
            var pipeline = AsyncPipelineExtensionsTests.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(2);

            await trainingSet.RunAsync(CancellationToken.None);

            var results = classifier.Classify(new TestData.Pirate()
            {
                Age = 72,
                Gold = 12,
                IsCaptain = true
            });

            Assert.That(results.Count(), Is.GreaterThan(0));
        }
    }
}
