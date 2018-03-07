using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
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
            var result = _classifier.Classify(new BiGram(word));

            Assert.That(result.Any());
        }

        private async Task WhenTrainingProcedureIsRun(int epochs = 1)
        {
            await _trainingSet.RunAsync(CancellationToken.None, epochs);
        }

        private void WhenSoftmaxNetworkClassifierAttached(int hiddenLayerSize = 64)
        {
            _classifier = _trainingSet.AttachMultilayerNetworkClassifier(b =>
            {
                b.AddHiddenLayer(new LayerSpecification(hiddenLayerSize, Activators.None(), LossFunctions.Square))
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