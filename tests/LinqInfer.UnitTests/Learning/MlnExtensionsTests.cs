using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning
{
    [TestFixture]
    public class MlnExtensionsTests
    {
        [Test]
        public async Task OpenAsMultilayerNetworkClassifier_ExportedClassifierWithExpression()
        {
            var data = Enumerable.Range(0, 10).Select(n => (x: n, y: n * 7));

            var pipeline = await data
                .CreatePipeline(x => new BitVector(x.x > 5, x.y > 14), 2)
                .CentreAndScaleAsync();

            var outputs = new[] { "a", "b" };

            var trainingSet = pipeline.AsTrainingSet(x => x.x > 5 ? outputs[0] : outputs[1], outputs);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(2);
            });

            await trainingSet.RunAsync(CancellationToken.None, 550);

            var doc = classifier.ExportData();

            var classifier2 = doc.OpenAsMultilayerNetworkClassifier<(int x, int y), string>();

            var results = classifier2.Classify((3, 33));

            Assert.That(results.Any());
        }

        [Test]
        public async Task AttachMultilayerNetworkClassifier_SoftmaxClassifierWithLinearDataSet_ClassifiesCorrectly()
        {
            var data = new[]
            {
                new
                {
                    x = 10,
                    y = 15,
                    classification = "a"
                },
                new
                {
                    x = 11,
                    y = 23,
                    classification = "a"
                },
                new
                {
                    x = -11,
                    y = -23,
                    classification = "b"
                },
                new
                {
                    x = -15,
                    y = -25,
                    classification = "b"
                }
            };

            var cancel = new CancellationTokenSource();

            var trainingSet = await data.AsAsyncEnumerator()
                .BuildPipelineAsync(cancel.Token)
                .CentreAndScaleAsync(LinqInfer.Maths.Range.ZeroToOne)
                .AsTrainingSetAsync(x => x.classification, cancel.Token);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(4, p =>
                {
                    p.HaltingFunction = (_, s) => s.AverageError < 0.4;
                    p.LearningRate = 0.005;
                });
            });

            await trainingSet.RunAsync(cancel.Token, 550);

            var results = classifier.Classify(new
            {
                x = 10,
                y = 10,
                classification = "?"
            });

            var exportedNetwork = classifier.ExportData();

            Assert.That(exportedNetwork, Is.Not.Null);

            TestFixtureBase.SaveArtifact("nn-350.xml", exportedNetwork.ExportAsXml().Save);

            var hypothesis = results.ToHypothetical();

            Assert.That(hypothesis.MostProbable(), Is.EqualTo("a"));
        }

        [Test]
        public async Task AttachMultilayerNetworkClassifier_WithExpression_LoadsAsExpected()
        {
            var pipeline = Enumerable.Range(1, 100)
                .Select(n => new double[]
                    {n % 3, n + 1, n + 2})
                .AsAsyncEnumerator()
                .CreatePipeline(x => ColumnVector1D.Create(x), 3);

            var trainingSet = pipeline.AsTrainingSet(x => x[0], 0, 1, 2);

            await VerifyTrainingSetAsync(trainingSet, new double[] {1, 2, 3});
        }

        [Test]
        public async Task AttachMultilayerNetworkClassifier_ThenReattach_LoadsAsExpected()
        {
            var pipeline = TestSamples.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(x => x.Category, "a", "b");
            
            await VerifyTrainingSetAsync(trainingSet, new TestData.Pirate());
        }

        static async Task VerifyTrainingSetAsync<TIn, TClass>(IAsyncTrainingSet<TIn, TClass> trainingSet, TIn testSample)
            where TClass : IEquatable<TClass>
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b =>
                b.ConfigureSoftmaxNetwork(4)
            );

            await trainingSet.RunAsync(CancellationToken.None);

            var data = classifier.ExportData();

            var classifier2 = trainingSet.AttachMultilayerNetworkClassifier(data);

            Assert.That(classifier2, Is.Not.Null);

            var result = classifier2.Classify(testSample).First();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task AttachMultilayerNetworkClassifier_XorSample_ClassifiesAsExpected()
        {
            var xor1 = new XorNode() { X = true, Y = false };
            var xor2 = new XorNode() { X = false, Y = false };
            var xor3 = new XorNode() { X = true, Y = true };
            var xor4 = new XorNode() { X = false, Y = true };

            var samples = new[] { xor1, xor2, xor3, xor4 };

            var pipeline = await samples.AsAsyncEnumerator().BuildPipelineAsync(CancellationToken.None);
            var trainingSet = pipeline.AsTrainingSet(x => x.Output, true, false);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(b => b.ConfigureSoftmaxNetwork(2));

            await trainingSet.RunAsync(CancellationToken.None, 100);

            var classResults1 = classifier.Classify(xor1).First();
            var classResults2 = classifier.Classify(xor2).First();
            var classResults3 = classifier.Classify(xor3).First();
            var classResults4 = classifier.Classify(xor4).First();

            Assert.That(classResults1.ClassType == xor1.Output);
            Assert.That(classResults2.ClassType == xor2.Output);
            Assert.That(classResults3.ClassType == xor3.Output);
            Assert.That(classResults4.ClassType == xor4.Output);
        }

        [Test]
        public async Task BuildAndAttachMultilayerNetworkClassifier_ExportData()
        {
            var pipeline = TestSamples.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                    .AddHiddenLayer(8, Activators.None())
                    .AddSoftmaxOutput();
            });

            await trainingSet.RunAsync(CancellationToken.None);

            var data = classifier.ExportData();

            Console.WriteLine(data.ExportAsXml().ToString());
        }

        [Test]
        public async Task BuildAndAttachMultilayerNetworkClassifier_WhenSoftmaxSpecification_ThenReturnsClassifier()
        {
            var pipeline = TestSamples.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                    .AddHiddenLayer(8, Activators.None())
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
            var pipeline = TestSamples.CreatePipeline();

            var trainingSet = pipeline.AsTrainingSet(p => p.Category, "a", "b");

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(builder =>
            {
                builder
                .AddHiddenSigmoidLayer(6)
                .ConfigureOutput(LossFunctions.Square);
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