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

        public IEnumerable<Task<IList<ObjectVector<T>>>> ExtractBatches()
        {
            foreach (var dataTask in _dataLoader)
            {
                var f = new Func<Task<IList<ObjectVector<T>>>>(async () =>
                {
                    var rawData = await dataTask;

                    return rawData
                        .Select(x => new ObjectVector<T>(x, _featureExtractor.ExtractIVector(x)))
                        .ToList();
                });

                yield return f();
            }
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