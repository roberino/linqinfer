using LinqInfer.Learning;
using LinqInfer.Maths;
using LinqInfer.Utility;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.ImageLearningTests
{
    [TestFixture]
    public class ImageLearningExamples
    {
        const int size = 15;

        [Test]
        public async Task GivenBitmapData_WhenSoftmaxNetworkTrained_ClassifierCreated()
        {
            var chars = new[] { 'O', 'i', 'X' };
            var bitmapDataSource = GivenSampleData(chars);
            var token = GivenTimeout(180);

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

            ThenClassifierCanClassify(chars, classifier);
        }

        static void ThenClassifierCanClassify(char[] chars, Learning.Classification.NeuralNetworks.INetworkClassifier<char, ImageSampleGeneration.Letter> classifier)
        {
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

        IReadOnlyCollection<ImageSampleGeneration.Letter> GivenSampleData(params char[] chars)
        {
            return chars
                .Letters(size, FontFamily.GenericMonospace)
                .Concat(chars.Letters(size, FontFamily.GenericSansSerif))
                .RandomOrder()
                .ToList();
        }

        CancellationToken GivenTimeout(int seconds)
        {
            var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
            return tokenSource.Token;
        }
    }
}