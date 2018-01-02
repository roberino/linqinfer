using LinqInfer.Learning;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Learning
{
    [TestFixture]
    public class LinearClassifierExtensionsTests
    {
        [Test]
        public async Task AttachLinearSoftmaxClassifier_WhenTrainingSetRun_ReturnsClassifier()
        {
            var pipeline = AsyncPipelineExtensionsTests.CreatePipeline();
            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachLinearSoftmaxClassifier();

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
