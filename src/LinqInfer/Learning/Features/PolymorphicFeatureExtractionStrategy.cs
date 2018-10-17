using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    class PolymorphicFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
    {
        readonly int _maxVectorSize;

        public PolymorphicFeatureExtractionStrategy(int maxVectorSize = 1024)
        {
            _maxVectorSize = maxVectorSize;
            Priority = 0;
        }

        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && 
                   propertyExtractor.HasValue;
        }

        public override IAsyncBuilderSink<T, IFloatingPointFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder(_maxVectorSize);
        }

        class Builder : IAsyncBuilderSink<T, IFloatingPointFeatureExtractor<T>>
        {
            readonly PolymorphicFeatureExtractor<T> _polymorphicFeatureExtractor;

            public Builder(int maxVectorSize)
            {
                _polymorphicFeatureExtractor = new PolymorphicFeatureExtractor<T>(maxVectorSize);
            }

            public bool CanReceive => !_polymorphicFeatureExtractor.CapacityReached;

            public Task<IFloatingPointFeatureExtractor<T>> BuildAsync()
            {
                _polymorphicFeatureExtractor.Resize();

                return Task.FromResult<IFloatingPointFeatureExtractor<T>>(_polymorphicFeatureExtractor);
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                foreach (var item in dataBatch.Items)
                {
                    _polymorphicFeatureExtractor.MapTypeInstance(item);
                }

                return Task.CompletedTask;
            }
        }
    }
}