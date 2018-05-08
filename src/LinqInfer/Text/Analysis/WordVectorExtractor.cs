using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    internal class WordVectorExtractor
    {
        public async Task<LabelledMatrix<string>> ExtractVectorsAsync(IAsyncTrainingSet<BiGram, string> trainingSet, CancellationToken cancellationToken, int vectorSize)
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            return trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v)
                  .ToMatrix();
        }

        public async Task<LabelledMatrix<string>> ExtractVectorsAsync(IAsyncTrainingSet<WordData, string> trainingSet, CancellationToken cancellationToken, int vectorSize)
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            return trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v)
                  .ToMatrix();
        }

        Action<FluentNetworkBuilder> NetworkBuilder(int vectorSize)
        {
            return b => b
               .ParallelProcess()
               .ConfigureLearningParameters(p =>
               {
                   p.NeverHalt();
               })
               .AddHiddenLayer(new LayerSpecification(vectorSize, Activators.None(), LossFunctions.Square))
               .AddSoftmaxOutput();
        }
    }
}