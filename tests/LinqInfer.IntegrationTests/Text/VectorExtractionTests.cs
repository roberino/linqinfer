using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class VectorExtractionTests
    {
        private IAsyncTrainingSet<BiGram, string> _trainingSet;
        private IDynamicClassifier<string, BiGram> _classifier;

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

            using (var fs = File.OpenWrite(@"c:\dev\nn.xml"))
            {
                doc.ExportAsXml().Save(fs);
            }
            using (var fs = File.OpenWrite(@"c:\dev\nn.dat"))
            {
                doc.Save(fs);
            }

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

            tokenSource.CancelAfter(TimeSpan.FromSeconds(30));

            var stats = _trainingSet.TrackStatistics();
            
            await _trainingSet.RunAsync(tokenSource.Token, epochs);

            Console.WriteLine($"Items received: {stats.Output.ItemsReceived}");
            Console.WriteLine($"Batches received: {stats.Output.BatchesReceived}");
            Console.WriteLine($"Average items per second: {stats.Output.AverageItemsPerSecond}");
            Console.WriteLine($"Average batches per second: {stats.Output.AverageBatchesPerSecond}");
        }

        private void WhenSoftmaxNetworkClassifierAttached(int hiddenLayerSize = 64)
        {
            _classifier = _trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b
                .ParallelProcess()
                .AddHiddenLayer(new LayerSpecification(hiddenLayerSize, Activators.None(), LossFunctions.Square))
                .ConfigureOutputLayer(Activators.None(), LossFunctions.CrossEntropy)
                .TransformOutput(x => new Softmax(x));
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