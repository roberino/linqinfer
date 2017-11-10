using LinqInfer.Utility;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class AsyncFeatureProcessingPipeline<T>
        : IAsyncFeatureProcessingPipeline<T>
        where T : class
    {
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly IAsyncEnumerator<T> _dataLoader;

        internal AsyncFeatureProcessingPipeline(IAsyncEnumerator<T> asyncDataLoader, IFloatingPointFeatureExtractor<T> featureExtractor)
        {
            _dataLoader = asyncDataLoader ?? throw new ArgumentNullException(nameof(asyncDataLoader));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
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