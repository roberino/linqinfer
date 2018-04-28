using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Tests.Learning;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests
{
    [TestFixture]
    public class ImageLearningExamples
    {
        [Test]
        public async Task TrainSoftmaxNetwork()
        {
            int size = 15;

            var chars = ImageSampleGeneration.Characters('A', 'F').ToArray();
            // var chars = new[] {'A', 'C', '.'};

            var bitmapDataSource = chars
                .Letters(size, FontFamily.GenericMonospace)
                .RandomOrder()
                .ToList();

            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var token = tokenSource.Token; //CancellationToken.None;

            var trainingSet = await bitmapDataSource
                .AsAsyncEnumerator()
                .CreatePipeline(l => l.VectorData, size * size)
                .CentreAndScaleAsync(Range.ZeroToOne)
                .AsTrainingSetAsync(l => l.Character, token);

            var network = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(size * size * 2, p => p.MinimumError = 0.05);
            });

            await trainingSet.RunAsync(token, 100);

            foreach (var unknownLetter in chars
                .Letters(size, FontFamily.GenericSerif)
                .RandomOrder())
            {
                var result = network.Classify(unknownLetter);

                Console.WriteLine($"{unknownLetter.Character}");

                foreach (var item in result)
                {
                    Console.WriteLine($"- {item.ClassType}={item.Score}");
                }

                Console.WriteLine();
            }
        }
    }
}