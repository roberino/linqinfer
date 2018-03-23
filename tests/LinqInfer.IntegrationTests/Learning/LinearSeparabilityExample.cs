using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.IntegrationTests.Learning
{
    [TestFixture]
    public class LinearSeparabilityExample
    {
        [Test]
        public async Task WhenGivenSoftmax()
        {
            var dataX0 = Functions.NormalRandomDataset(3, 10);
            var dataX1 = Functions.NormalRandomDataset(0.6, 78);
            var dataY0 = Functions.NormalRandomDataset(2, 98);
            var dataY1 = Functions.NormalRandomDataset(7, 12);

            var testX0 = Functions.NormalRandomDataset(3, 10);
            var testY0 = Functions.NormalRandomDataset(2, 98);
            var testX1 = Functions.NormalRandomDataset(0.6, 78);
            var testY1 = Functions.NormalRandomDataset(7, 12);

            var c0 = dataX0.Zip(dataY0, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C0"
            });

            var testSet = testX0.Zip(testY0, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C0"
            }).Concat(testX1.Zip(testY1, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C1"
            }))
            .RandomOrder()
            .AsQueryable()
            .CreatePipeline()
            .CentreFeatures()
            .ScaleFeatures(new Range(1, -1))
            .AsTrainingSet(x => x.cls);

            var c1 = dataX1.Zip(dataY1, (x, y) => new
            {
                x = x,
                y = y,
                cls = "C1"
            });

            var pipeline = await c0.Concat(c1)
                .RandomOrder()
                .AsQueryable()
                .AsAsyncEnumerator()
                .BuildPipelineAsync(CancellationToken.None);

            pipeline = await pipeline.CentreAndScaleAsync();

            var trainingSet = pipeline
                .AsTrainingSet(c => c.cls, "C0", "C1");

            var classifier = trainingSet
                .AttachMultilayerNetworkClassifier(b =>
            {
                b.ParallelProcess()
                .ConfigureLearningParameters(p =>
                {
                    p.LearningRate = 0.005;
                    p.Momentum = 0.1;
                    p.MinimumError = 0.01;
                })
                .AddHiddenLayer(new LayerSpecification(4, Activators.None(), LossFunctions.Square))
                .AddSoftmaxOutput();
            });

            await trainingSet.RunAsync(CancellationToken.None);

            var score = classifier.ClassificationAccuracyPercentage(testSet);

            Console.WriteLine(score);

            Assert.That(score, Is.GreaterThan(0));
        }
    }
}
