using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using LinqInfer.Text.VectorExtraction;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text
{
    public static class AsyncTextExtensions
    {
        public static async Task<IAsyncFeatureProcessingPipeline<TInput>> BuildMultifunctionPipelineAsync<TInput>(
            this ITransformingAsyncBatchSource<TInput> asyncEnumerator,
            CancellationToken cancellationToken,
            int maxVocabularySize = 1000,
            ITokeniser tokeniser = null)
            where TInput : class
        {
            var defaultStrategy = new DefaultFeatureExtractionStrategy<TInput>();
            var semanticStrategy = new TextFeatureExtractionStrategy<TInput>(maxVocabularySize, tokeniser);
            var categoricalStrategy = new CategoricalFeatureExtractionStrategy<TInput>();

            var builder = new FeatureExtractorBuilder<TInput>(typeof(TInput), defaultStrategy, semanticStrategy, categoricalStrategy);

            var fe = await builder.BuildAsync(asyncEnumerator, cancellationToken);

            return new AsyncFeatureProcessingPipeline<TInput>(asyncEnumerator, fe);
        }
    }
}