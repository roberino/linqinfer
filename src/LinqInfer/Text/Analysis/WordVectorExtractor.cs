using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text.Analysis
{
    class WordVectorExtractor
    {
        public async Task<VectorExtractionResult> ExtractVectorsAsync<T>(
            IAsyncTrainingSet<T, string> trainingSet, 
            CancellationToken cancellationToken, 
            int vectorSize)
            where T : class 
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));
            
            return await ExtractVectorsAsync(trainingSet, cancellationToken, classifier);
        }

        public async Task<VectorExtractionResult> ExtractVectorsAsync<T>(
            IAsyncTrainingSet<T, string> trainingSet,
            CancellationToken cancellationToken,
            PortableDataDocument data) where T : class
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(data);

            return await ExtractVectorsAsync(trainingSet, cancellationToken, classifier);
        }

        static Action<IConvolutionalNetworkBuilder> NetworkBuilder(int vectorSize)
        {
            return b => b.ConfigureSoftmaxNetwork(vectorSize, c =>
            {
                c.LearningRate = 0.01;
                c.NeverHalt();
            });
        }

        async Task<VectorExtractionResult> ExtractVectorsAsync<T>(IAsyncTrainingSet<T, string> trainingSet,
            CancellationToken cancellationToken, INetworkClassifier<string, T> classifier)
            where T : class
        {
            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            var vectors = trainingSet
                .OutputMapper
                .FeatureMetadata
                .Zip(mln.Children.Last().Vectors, (f, v) => new {f, v})
                .ToDictionary(x => x.f.Label, v => v.v)
                .ToMatrix();

            return new VectorExtractionResult(classifier.ExportData(), vectors);
        }
    }
}