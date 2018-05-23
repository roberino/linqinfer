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
        public async Task<VectorExtractionResult<BiGram>> ExtractVectorsAsync(IAsyncTrainingSet<BiGram, string> trainingSet, CancellationToken cancellationToken, int vectorSize)
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            var vectors = trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v)
                  .ToMatrix();

            return new VectorExtractionResult<BiGram>(classifier, vectors);
        }

        public async Task<VectorExtractionResult<WordData>> ExtractVectorsAsync(IAsyncTrainingSet<WordData, string> trainingSet, CancellationToken cancellationToken, int vectorSize)
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            var vectors = trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v)
                  .ToMatrix();

            return new VectorExtractionResult<WordData>(classifier, vectors);
        }

        static Action<FluentNetworkBuilder> NetworkBuilder(int vectorSize)
        {
            return b => b.ConfigureSoftmaxNetwork(vectorSize, c =>
            {
                c.LearningRate = 0.01;
                c.NeverHalt();
            });
        }
    }
}