using LinqInfer.Data.Pipes;
using System.Linq;
using System.Threading.Tasks;

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

        public override Task<IFloatingPointFeatureExtractor<T>> BuildAsync(IAsyncEnumerator<T> samples)
        {
            var props = Properties;

            return Task.FromResult<IFloatingPointFeatureExtractor<T>>(new DelegatingFloatingPointFeatureExtractor<T>(
                x => props.Select(p => p.ConversionFunction(x)).ToArray(),
                props.Count,
                Feature.CreateDefaults(props.Select(p => p.Property.Name)))
                );
        }
    }
}
