using LinqInfer.Data.Pipes;
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
        private IAsyncTrainingSet<BiGram, string> _bigramTrainingSet;
        private IAsyncTrainingSet<WordVector, string> _aggTrainingSet;
        private INetworkClassifier<string, BiGram> _classifier;

        [Test]
        public async Task WhenGivenCbow_ThenClassifierCanBeConstructed()
        {
            await GivenAnAsyncTextTrainingSet();

            WhenSoftmaxNetworkClassifierAttached();

            await WhenTrainingProcedureIsRun();

            ThenClassifierCanClassifyWords("bold");
        }

        [Test]
        public async Task WhenGivenCbow_ThenVectorsCanBeExtracted()
        {
            await GivenAnAsyncTextTrainingSet();

            await ThenBigramVectorsCanBeExtracted();
        }

        [Test]
        public async Task WhenGivenAggregatedSet_ThenClassifierCanBeConstructed()
        {
            await GivenAnAggregatedAsyncTrainingSet();
            
            await ThenAggrVectorsCanBeExtracted();
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

            var stats = _bigramTrainingSet.TrackStatistics();
            
            await stats.Pipe.RunAsync(tokenSource.Token, epochs);

            LogPipeStats(stats.Output);
        }

        private void LogPipeStats(IPipeStatistics stats)
        {
            Console.WriteLine($"Elapsed: {stats.Elapsed}");
            Console.WriteLine($"Batches received: {stats.BatchesReceived}");
            Console.WriteLine($"Items received: {stats.ItemsReceived}");
            Console.WriteLine($"Average items per second: {stats.AverageItemsPerSecond}");
            Console.WriteLine($"Average batches per second: {stats.AverageBatchesPerSecond}");
        }

        private void WhenSoftmaxNetworkClassifierAttached(int hiddenLayerSize = 64)
        {
            void NetworkBuilder(FluentNetworkBuilder b)
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
            }

            _classifier = _bigramTrainingSet?.AttachMultilayerNetworkClassifier(NetworkBuilder);

        }

        private async Task ThenAggrVectorsCanBeExtracted()
        {
            var vects = await _aggTrainingSet.ExtractVectorsAsync(CancellationToken.None, 64);

            foreach(var kv in vects)
            {
                Console.WriteLine($"{kv.Key}, {kv.Value.ToColumnVector().ToCsv()}");
            }
        }

        private async Task ThenBigramVectorsCanBeExtracted()
        {
            var vects = await _bigramTrainingSet.ExtractVectorsAsync(CancellationToken.None, 64);

            foreach (var kv in vects)
            {
                Console.WriteLine($"{kv.Key}, {kv.Value.ToColumnVector().ToCsv()}");
            }
        }

        private async Task<IAsyncTrainingSet<WordVector, string>> GivenAnAggregatedAsyncTrainingSet()
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

            var keyTerms = await corpus.ExtractKeyTermsAsync(CancellationToken.None);
            
            var cbow = corpus.CreateAsyncContinuousBagOfWords(keyTerms);
            
            _bigramTrainingSet = cbow.AsBiGramAsyncTrainingSet();

            return _bigramTrainingSet;
        }
    }
}