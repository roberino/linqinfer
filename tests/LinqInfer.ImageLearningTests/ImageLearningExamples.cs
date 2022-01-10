using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.ImageLearningTests
{
    [TestFixture]
    public class ImageLearningExamples
    {
        [Test]
        public async Task TrainSoftmaxNetworkAsync()
        {
            const int size = 15;

            var chars = new[] {'O', 'i', 'X'};

            var bitmapDataSource = chars
                .Letters(size, FontFamily.GenericMonospace)
                .Concat(chars.Letters(size, FontFamily.GenericSansSerif))
                .RandomOrder()
                .ToList();
            
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(180));
            var token = tokenSource.Token;
            
            var trainingSet = await bitmapDataSource
                .AsAsyncEnumerator()
                .CreatePipeline(l => l.VectorData, size * size)
                .CentreAndScaleAsync(Maths.Range.ZeroToOne)
                .AsTrainingSetAsync(l => l.Character, token);

            var network = trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.ConfigureSoftmaxNetwork(size * size * 2, p =>
                {
                    p.ErrorHistoryCount = 150;
                    p.HaltingFunction = (_, s) =>
                    {
                        return s.AverageError < 0.5 || s.Trend > 0.1;
                    };
                    p.LearningRate = 0.05;

                });
            });

            await trainingSet.RunAsync(token, 300);

            var data = network.ExportData();

            var classifier = data.OpenAsMultilayerNetworkClassifier<ImageSampleGeneration.Letter, char>();

            foreach (var unknownLetter in chars
                .Letters(size, FontFamily.GenericSerif))
            {
                var result = classifier.Classify(unknownLetter);

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