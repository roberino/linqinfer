using LinqInfer.Data.Pipes;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Data;
using System.Threading;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    internal class DefaultFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
    {
        public DefaultFeatureExtractionStrategy()
        {
            Priority = -1;
        }

        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && 
                propertyExtractor.ConversionFunction != null;
        }

        public override IBuilder<T, IFloatingPointFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder(Properties);
        }

        private class Builder : IBuilder<T, IFloatingPointFeatureExtractor<T>>
        {
            private readonly IList<PropertyExtractor<T>> _properties;

            public Builder(IList<PropertyExtractor<T>> properties)
            {
                _properties = properties;
            }

            public bool CanReceive => true;

            public Task<IFloatingPointFeatureExtractor<T>> BuildAsync()
            {
                return Task.FromResult<IFloatingPointFeatureExtractor<T>>(new DelegatingFloatingPointFeatureExtractor<T>(
                   x => _properties.Select(p => p.ConversionFunction(x)).ToArray(),
                   _properties.Count,
                   Feature.CreateDefaults(_properties.Select(p => p.Property.Name)))
                   );
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                return Task.FromResult(true);
            }
        }
    }
}