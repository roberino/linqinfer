using LinqInfer.Data.Pipes;
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
        IAsyncTrainingSet<BiGram, string> _bigramTrainingSet;
        IAsyncTrainingSet<WordData, string> _aggTrainingSet;

        [Test]
        public async Task WhenGivenCbow_ThenVectorsCanBeExtracted()
        {
            await GivenAnAsyncTextTrainingSet();

            await ThenBigramVectorsCanBeExtracted();

        }

        void LogPipeStats(IPipeStatistics stats)
        {
            Console.WriteLine($"Elapsed: {stats.Elapsed}");
            Console.WriteLine($"Batches received: {stats.BatchesReceived}");
            Console.WriteLine($"Items received: {stats.ItemsReceived}");
            Console.WriteLine($"Average items per second: {stats.AverageItemsPerSecond}");
            Console.WriteLine($"Average batches per second: {stats.AverageBatchesPerSecond}");
        }

        async Task ThenBigramVectorsCanBeExtracted()
        {
            var result = await _bigramTrainingSet.ExtractVectorsAsync(CancellationToken.None, 64);

            var vects = result.Vectors;

            await vects.WriteAsCsvAsync(Console.Out);

            using (var fs = File.OpenWrite(@"C:\dev\vect.csv"))
            using (var writer = new StreamWriter(fs))
            {
                await vects.LabelledCosineSimularityMatrix.WriteAsCsvAsync(writer);
            }
        }

        async Task<IAsyncTrainingSet<BiGram, string>> GivenAnAsyncTextTrainingSet()
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