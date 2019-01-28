using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    class DefaultFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
        where T : class
    {
        public DefaultFeatureExtractionStrategy()
        {
            Priority = -1;
        }

        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && 
                propertyExtractor.HasValue;
        }

        public override IAsyncBuilderSink<T, IVectorFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder();
        }

        class Builder : IAsyncBuilderSink<T, IVectorFeatureExtractor<T>>
        {
            public bool CanReceive => false;

            public Task<IVectorFeatureExtractor<T>> BuildAsync()
            {
                return Task.FromResult<IVectorFeatureExtractor<T>>(
                    new ObjectFeatureExtractor<T>());
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}