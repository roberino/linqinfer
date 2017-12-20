using LinqInfer.Data.Pipes;
using LinqInfer.Maths.Probability;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Data;

namespace LinqInfer.Learning.Features
{
    internal class CategoricalFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
    {
        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && propertyExtractor.FeatureMetadata.Model == DistributionModel.Categorical;
        }

        public override IBuilder<T, IFloatingPointFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder(Properties);
        }

        private class Builder : IBuilder<T, IFloatingPointFeatureExtractor<T>>
        {
            private readonly IDictionary<string, long> _values;
            private readonly IEnumerable<PropertyExtractor<T>> _properties;

            public Builder(IEnumerable<PropertyExtractor<T>> properties)
            {
                _values = new Dictionary<string, long>();
                _properties = properties;
            }

            public Task<IFloatingPointFeatureExtractor<T>> BuildAsync()
            {
                var set = new HashSet<string>(_values.Keys);

                var fe = new CategoricalFeatureExtractor<T, string>(GetValue, Feature.CreateDefaults(_properties.Select(p => p.Property.Name), DistributionModel.Categorical), set);

                return Task.FromResult<IFloatingPointFeatureExtractor<T>>(fe);
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                foreach (var v in dataBatch.Items)
                {
                    _values[GetValue(v)] = 1;
                }

                return Task.FromResult(true);
            }

            private string GetValue(T item)
            {
                return _properties
                    .Select(p => p.Property.GetValue(item))
                    .Aggregate(new StringBuilder(), (s, v) => (s.Length > 0 ? s.Append('/') : s).Append(Convert(v)))
                    .ToString();
            }

            private static string Convert(object value)
            {
                if (value == null) return string.Empty;

                return value.ToString();
            }
        }
    }
}