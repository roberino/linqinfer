using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class MlnExtensionsTests
    {
        [Test]
        public async Task BuildAndAttachMultilayerNetworkClassifier_WhenSoftmaxSpecification_ThenReturnsClassifier()
        {
            var pipeline = AsyncPipelineExtensionsTests.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                    .AddHiddenLayer(new LayerSpecification(8, Activators.None(), LossFunctions.Square))
                    .AddSoftmaxOutput();
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

        [Test]
        public async Task BuildAndAttachMultilayerNetworkClassifier_WhenCustomSpecification_ThenReturnsClassifier()
        {
            var pipeline = AsyncPipelineExtensionsTests.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                .AddHiddenSigmoidLayer(6)
                .ConfigureOutputLayer(Activators.None(), LossFunctions.Square);
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

        [Test]
        public void ToMultilayerNetworkClassifier_UsingParametersSimpleSample_ClassifiesAsExpected()
        {
            var classifier = TestData
                .CreatePirates()
                .AsQueryable()
                .CreatePipeline()
                .AsTrainingSet(p => p.Age > 25 ? "old" : "young")
                .ToMultilayerNetworkClassifier(8)
                .Execute();

            var results = classifier.Classify(new TestData.Pirate()
            {
                Gold = 120,
                Age = 5,
                IsCaptain = false,
                Ships = 1
            });

            Assert.That(results.Count(), Is.GreaterThan(0));
        }
    }
}
