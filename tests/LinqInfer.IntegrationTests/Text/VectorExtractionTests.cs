using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class VectorExtractionTests
    {
        private IAsyncTrainingSet<BiGram, string> _bigramTrainingSet;
        private IAsyncTrainingSet<WordData, string> _aggTrainingSet;

        [Test]
        public async Task WhenGivenCbow_ThenVectorsCanBeExtracted()
        {
            await GivenAnAsyncTextTrainingSet();

            await ThenBigramVectorsCanBeExtracted();
        }

        [Test]
        public async Task WhenGivenAggregatedSet_ThenVectorsCanBeExtracted()
        {
            await GivenAnAggregatedAsyncTrainingSet();
            
            await ThenAggrVectorsCanBeExtracted();
        }

        private void LogPipeStats(IPipeStatistics stats)
        {
            Console.WriteLine($"Elapsed: {stats.Elapsed}");
            Console.WriteLine($"Batches received: {stats.BatchesReceived}");
            Console.WriteLine($"Items received: {stats.ItemsReceived}");
            Console.WriteLine($"Average items per second: {stats.AverageItemsPerSecond}");
            Console.WriteLine($"Average batches per second: {stats.AverageBatchesPerSecond}");
        }

        private async Task ThenAggrVectorsCanBeExtracted()
        {
            var vects = await _aggTrainingSet.ExtractVectorsAsync(
                CancellationToken.None, 64);

            await vects.WriteAsCsvAsync(Console.Out);
        }

        private async Task ThenBigramVectorsCanBeExtracted()
        {
            var vects = await _bigramTrainingSet.ExtractVectorsAsync(CancellationToken.None, 64);

            await vects.WriteAsCsvAsync(Console.Out);

            using (var fs = File.OpenWrite(@"C:\dev\vect.csv"))
            using (var writer = new StreamWriter(fs))
            {
                await vects.LabelledCosineSimularityMatrix.WriteAsCsvAsync(writer);
            }
        }

        private async Task<IAsyncTrainingSet<WordData, string>> GivenAnAggregatedAsyncTrainingSet()
        {
            var corpus = CorpusDataSource.GetCorpus(5000);

            var keyTerms = await corpus.ExtractKeyTermsAsync(CancellationToken.None);

            var cbow = corpus.CreateAsyncContinuousBagOfWords(keyTerms);

            _aggTrainingSet = await cbow.CreateAggregatedTrainingSetAsync(CancellationToken.None);

            return _aggTrainingSet;
        }

        private async Task<IAsyncTrainingSet<BiGram, string>> GivenAnAsyncTextTrainingSet()
        {
            var corpus = CorpusDataSource.GetCorpus();

            var targetWords = new HashSet<string>(new [] { "good", "bad", "ugly", "pretty",
                "man", "woman", "king", "queen", "animal", "child", "goat",
                "clean", "dirty", "filthy", "pure", "female", "male", "big", "small",
                "strong", "weak", "health", "sick", "empire", "president",
                "pain", "pleasure", "boy", "girl", "hot", "cold", "white", "black",
                "big", "small" });

            var targetVocab = new SemanticSet(targetWords);
            
            var cbow = corpus.CreateAsyncContinuousBagOfWords(targetVocab, new EnglishDictionary());
            
            _bigramTrainingSet = cbow.AsBiGramAsyncTrainingSet();

            await Task.Delay(0);

            return _bigramTrainingSet;
        }
    }
}