using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Text.Analysis;
using LinqInfer.Text.VectorExtraction;
using LinqInfer.Utility;

namespace LinqInfer.Text
{
    public static class ClassificationExtensions
    {
        public static IAsyncTrainingSet<string, string> CreateTextTimeSequenceTrainingSet(this ICorpus corpus, ISemanticSet vocabulary)
        {
            var encoding = new OneHotTextEncoding<string>(vocabulary, t => t);

            var data = corpus.ReadBlocksAsync().TransformEachItem(t => t.Text.ToLowerInvariant());

            var pipeline = data.CreatePipeline(encoding);

            return pipeline.CreateCategoricalTimeSequenceTrainingSet(encoding.Encoder);
        }

        /// <summary>
        /// Creates a semantic neural network classifier which
        /// uses term frequency over known classifications of objects.
        /// </summary>
        /// <typeparam name="T">The input token type</typeparam>
        /// <param name="data">The data</param>
        /// <param name="classifyingFunction">A function which will return a class label for an object</param>
        /// <param name="maxVectorSize">The maximum number of terms (words) used to classify object</param>
        /// <returns>An object classifier</returns>
        public static IObjectClassifier<string, T> CreateSemanticClassifier<T>(this IQueryable<T> data,
            Expression<Func<T, string>> classifyingFunction, int maxVectorSize = 128) where T : class
        {
            var index = new DocumentIndex();
            var cf = classifyingFunction.Compile();
            var tokeniser = new ObjectTextExtractor<T>(index.Tokeniser);
            var objtokeniser = tokeniser.CreateObjectTextTokeniser();
            var docs = data.Select(x => new TokenisedTextDocument(cf(x), objtokeniser(x)));

            index.IndexDocuments(docs);

            var featureExtractor = index.CreateVectorExtractorByDocumentKey(objtokeniser, maxVectorSize);

            var outputs = data.GroupBy(classifyingFunction).Select(g => g.Key).ToArray();

            var trainingSet =
                data.AsAsyncEnumerator()
                    .CreatePipeline(featureExtractor)
                    .AsTrainingSet(classifyingFunction, outputs);

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(
                b =>
                {
                    b.ConfigureSoftmaxNetwork(
                        featureExtractor.VectorSize * 2,
                        lp => { lp.LearningRate = 0.01; });
                }
            );

            trainingSet.RunAsync(CancellationToken.None, 100).ConfigureAwait(true).GetAwaiter().GetResult();

            return classifier;
        }
    }
}
