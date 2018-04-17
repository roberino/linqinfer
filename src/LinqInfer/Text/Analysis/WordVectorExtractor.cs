using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Text.VectorExtraction;
using System;
using System.Collections.Generic;
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

            var doc = classifier.ToVectorDocument();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            return trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v)
                  .ToMatrix();
        }

        public async Task<LabelledMatrix<string>> ExtractVectorsAsync(IAsyncTrainingSet<WordVector, string> trainingSet, CancellationToken cancellationToken, int vectorSize)
        {
            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder(vectorSize));

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ToVectorDocument();

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

        public LabelledMatrix<string> Extract(ContinuousBagOfWords cbow, ISemanticSet widerVocabulary, int sampleSize = 10000)
        {
            var data = cbow.GetNGrams().SelectMany(c =>
                    c.ContextualWords
                    .Select(w => new BiGram(w.NormalForm(), c.TargetWord.NormalForm()))
                   )
                   .Take(sampleSize);

            var encoder = new OneHotTextEncoding<BiGram>(widerVocabulary, t => t.Input);

            var pipeline = data.AsQueryable().CreatePipeline(encoder);

            var trainingSet = pipeline.AsTrainingSet(t => t.Output);

            var classifier = new LinearSoftmaxClassifier(trainingSet.FeaturePipeline.VectorSize, trainingSet.OutputMapper.VectorSize);

            classifier.Train(trainingSet.ExtractTrainingVectorBatches().SelectMany(b => b), 0.0001f);

            return new LabelledMatrix<string>(classifier.Vectors, trainingSet.OutputMapper.FeatureMetadata.ToDictionary(m => m.Label, m => m.Index));
        }
    }
}