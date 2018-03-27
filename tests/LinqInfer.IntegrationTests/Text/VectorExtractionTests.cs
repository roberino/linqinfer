﻿using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class VectorExtractionTests
    {
        private IAsyncTrainingSet<BiGram, string> _trainingSet;
        private INetworkClassifier<string, BiGram> _classifier;

        [Test]
        public async Task WhenGivenCbow_ThenClassifierCanBeConstructed()
        {
            await GivenAnAsyncTextTrainingSet();

            WhenSoftmaxNetworkClassifierAttached();

            await WhenTrainingProcedureIsRun();

            ThenClassifierCanClassifyWords("bold");
        }

        private void ThenClassifierCanClassifyWords(string word)
        {
            var doc = _classifier.ToVectorDocument();
                                    
            var result = _classifier.Classify(new BiGram(word));

            Assert.That(result.Any());

            foreach(var item in result.OrderByDescending(x => x.Score))
            {
                Console.WriteLine($"{item.ClassType} = {item.Score}");
            }
        }

        private async Task WhenTrainingProcedureIsRun(int epochs = 1)
        {
            var tokenSource = new CancellationTokenSource();

            tokenSource.CancelAfter(TimeSpan.FromSeconds(60));

            var stats = _trainingSet.TrackStatistics();
            
            await stats.Pipe.RunAsync(tokenSource.Token, epochs);

            Console.WriteLine($"Elapsed: {stats.Output.Elapsed}");
            Console.WriteLine($"Batches received: {stats.Output.BatchesReceived}");
            Console.WriteLine($"Items received: {stats.Output.ItemsReceived}");
            Console.WriteLine($"Average items per second: {stats.Output.AverageItemsPerSecond}");
            Console.WriteLine($"Average batches per second: {stats.Output.AverageBatchesPerSecond}");
        }

        private void WhenSoftmaxNetworkClassifierAttached(int hiddenLayerSize = 64)
        {
            _classifier = _trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b
                .ParallelProcess()
                .ConfigureLearningParameters(p =>
                {
                    p.LearningRate = 0.2;
                    p.Momentum = 0.1;
                })
                .AddHiddenLayer(new LayerSpecification(hiddenLayerSize, Activators.None(), LossFunctions.Square))
                .AddSoftmaxOutput();
            });
        }

        private async Task<IAsyncTrainingSet<BiGram, string>> GivenAnAsyncTextTrainingSet()
        {
            var corpus = CorpusDataSource.GetCorpus();

            var keyTerms = await corpus.ExtractKeyTermsAsync(CancellationToken.None);
            
            var cbow = corpus.CreateAsyncContinuousBagOfWords(keyTerms);
            
            _trainingSet = cbow.AsBiGramAsyncTrainingSet();

            return _trainingSet;
        }
    }
}