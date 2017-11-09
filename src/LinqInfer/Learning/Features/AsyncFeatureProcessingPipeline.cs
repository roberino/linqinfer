using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class AsyncFeatureProcessingPipeline<T>
        : IAsyncFeatureProcessingPipeline<T>
        where T : class
    {
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly IEnumerable<Task<IList<T>>> _dataLoader;

        internal AsyncFeatureProcessingPipeline(IEnumerable<Task<IList<T>>> dataLoader, IFloatingPointFeatureExtractor<T> featureExtractor)
        {
            _dataLoader = dataLoader ?? throw new ArgumentNullException(nameof(dataLoader));
            _featureExtractor = featureExtractor ?? throw new ArgumentNullException(nameof(featureExtractor));
        }

        public AsyncEnumerator<ObjectVector<T>> ExtractBatches()
        {
            return _dataLoader
                .AsAsyncEnumerator()
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