using LinqInfer.Data.Serialisation;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Text.Analysis;
using LinqInfer.Text.VectorExtraction;
using LinqInfer.Utility;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using LinqInfer.Text.Indexing;

namespace LinqInfer.Text
{
    public static class ClassificationExtensions
    {
        public static INetworkClassifier<string, string> OpenTextClassifier(this PortableDataDocument existingClassifierData)
        {
            var extractorFactory = FeatureExtractorFactory<string>
                .Default
                .Register<OneHotTextEncoding<string>>(
                    x => OneHotTextEncoding<string>.Create(x.Data));

            return MultilayerNetworkObjectClassifier<string, string>.Create(existingClassifierData, extractorFactory);
        }

        public static IAsyncTrainingSet<string, string> CreateTimeSequenceTrainingSet(this ICorpus corpus, ISemanticSet vocabulary)
        {
            var encoding = new OneHotTextEncoding<string>(vocabulary, t => new [] { t });

            var data = corpus.ReadBlocksAsync().TransformEachItem(t => t.Text.ToLowerInvariant());

            var pipeline = data.CreatePipeline(encoding);

            return pipeline.CreateCategoricalTimeSequenceTrainingSet(encoding.Encoder);
        }
    }
}