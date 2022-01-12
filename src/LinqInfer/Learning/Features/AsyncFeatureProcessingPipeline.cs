using LinqInfer.Data.Pipes;
using LinqInfer.Maths;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    class AsyncFeatureProcessingPipeline<T>
        : AsyncPipe<ObjectVectorPair<T>>, IAsyncFeatureProcessingPipeline<T>
    {
        readonly TransformingFeatureExtractor<T> _featureExtractor;
        readonly ITransformingAsyncBatchSource<T> _dataLoader;

        internal AsyncFeatureProcessingPipeline(ITransformingAsyncBatchSource<T> asyncDataLoader, IVectorFeatureExtractor<T> featureExtractor)
            : base(ExtractBatches(asyncDataLoader, featureExtractor))
        {
            _dataLoader = asyncDataLoader ?? throw new ArgumentNullException(nameof(asyncDataLoader));
            _featureExtractor = featureExtractor as TransformingFeatureExtractor<T> ?? new TransformingFeatureExtractor<T>(featureExtractor);
        }

        public IVectorFeatureExtractor<T> FeatureExtractor => _featureExtractor;

        /// <summary>
        /// Centres and scales the data
        /// </summary>
        public async Task<IAsyncFeatureProcessingPipeline<T>> CentreAndScaleAsync(Maths.Range? range = null)
        {
            var minMaxMean = await MinMaxMeanVector.MinMaxAndMeanOfEachDimensionAsync(ExtractBatches().TransformEachItem(o => o.Vector));

            var transform = minMaxMean.CreateCentreAndScaleTransformation(range);

            return PreprocessWith(transform);
        }

        /// <summary>
        /// Preprocesses the data with the supplied transformation
        /// </summary>
        /// <param name="transformation">The vector transformation</param>
        /// <returns>The current <see cref="FeatureProcessingPipeline{T}"/></returns>
        public IAsyncFeatureProcessingPipeline<T> PreprocessWith(ISerialisableDataTransformation transformation)
        {
            _featureExtractor.AddTransform(transformation);

            return this;
        }

        public ITransformingAsyncBatchSource<ObjectVectorPair<T>> ExtractBatches()
        {
            return ExtractBatches(_dataLoader, _featureExtractor);
        }

        static ITransformingAsyncBatchSource<ObjectVectorPair<T>> ExtractBatches(ITransformingAsyncBatchSource<T> dataLoader, IVectorFeatureExtractor<T> fe)
        {
            return dataLoader
                .TransformEachBatch(b => b
                        .Select(x => new ObjectVectorPair<T>(x, fe.ExtractIVector))
                        .ToList());
        }
    }
}