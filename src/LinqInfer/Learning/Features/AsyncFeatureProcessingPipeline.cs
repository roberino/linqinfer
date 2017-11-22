using LinqInfer.Data;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class AsyncFeatureProcessingPipeline<T>
        : IAsyncFeatureProcessingPipeline<T>
        where T : class
    {
        private readonly MultiFunctionFeatureExtractor<T> _featureExtractor;
        private readonly IAsyncEnumerator<T> _dataLoader;

        internal AsyncFeatureProcessingPipeline(IAsyncEnumerator<T> asyncDataLoader, IFloatingPointFeatureExtractor<T> featureExtractor)
        {
            _dataLoader = asyncDataLoader ?? throw new ArgumentNullException(nameof(asyncDataLoader));
            _featureExtractor = new MultiFunctionFeatureExtractor<T>(featureExtractor);
        }

        public IFloatingPointFeatureExtractor<T> FeatureExtractor => _featureExtractor;

        /// <summary>
        /// Centres and scales the data
        /// </summary>
        public async Task<IAsyncFeatureProcessingPipeline<T>> CentreAndScaleAsync(Range? range = null)
        {
            var minMaxMean = await MinMaxMeanVector.MinMaxAndMeanOfEachDimensionAsync(ExtractBatches().TransformEachItem(o => o.VirtualVector));

            var transform = minMaxMean.CreateCentreAndScaleTransformation(range);

            return PreprocessWith(transform);
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public IAsyncFeatureProcessingPipeline<T> PreprocessWith(IVectorTransformation transformation)
        {
            _featureExtractor.PreprocessWith(transformation);

            return this;
        }

        public IAsyncEnumerator<ObjectVector<T>> ExtractBatches()
        {
            return _dataLoader
                .TransformEachBatch(b => b
                        .Select(x => new ObjectVector<T>(x, _featureExtractor.ExtractIVector(x)))
                        .ToList());
        }

        public ExecutionPipline<TResult> ProcessAsyncWith<TResult>(Func<IAsyncFeatureProcessingPipeline<T>, string, Task<TResult>> processor)
        {
            return new ExecutionPipline<TResult>((n) =>
            {
                var res = processor.Invoke(this, n);

                return res;
            }, (x, o) => true);
        }
    }
}