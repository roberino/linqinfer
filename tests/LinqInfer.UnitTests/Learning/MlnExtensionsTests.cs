using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
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
        public async Task BuildAndAttachMultilayerNetworkClassifier_WhenCustomSpecification_ThenReturnsClassifier()
        {
            var pipeline = AsyncPipelineExtensionsTests.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                .AddHiddenSigmoidLayer(6)
                .ConfigureOutputLayer(Activators.None(), LossFunctions.Default);
            });

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
