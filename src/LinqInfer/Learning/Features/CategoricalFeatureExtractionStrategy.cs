using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class CategoricalFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
    {
        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && propertyExtractor.FeatureMetadata.Model == FeatureVectorModel.Categorical;
        }

        public override IBuilder<T, IFloatingPointFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder(Properties);
        }

        private class Builder : IBuilder<T, IFloatingPointFeatureExtractor<T>>
        {
            private readonly IList<Tuple<PropertyExtractor<T>, IDictionary<string, long>>> _propertyLookups;

            public Builder(IEnumerable<PropertyExtractor<T>> properties)
            {
                _propertyLookups = properties
                    .Select(p =>
                        new Tuple<PropertyExtractor<T>, IDictionary<string, long>>
                            (p, new Dictionary<string, long>()))
                            .ToList();
            }

            public bool CanReceive => true;

            public Task<IFloatingPointFeatureExtractor<T>> BuildAsync()
            {
                var extractors = new List<IFloatingPointFeatureExtractor<T>>(_propertyLookups.Count);

                foreach (var plookup in _propertyLookups)
                {
                    var set = new HashSet<string>(plookup.Item2.Keys);

                    var fe = new CategoricalFeatureExtractor<T, string>(x => GetValue(x, plookup.Item1), Feature.CreateDefaults(new[] { plookup.Item1.Property.Name }, FeatureVectorModel.Categorical), set);

                    extractors.Add(fe);
                }

                return Task.FromResult<IFloatingPointFeatureExtractor<T>>(new MultiStrategyFeatureExtractor<T>(extractors.ToArray()));
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                foreach (var plookup in _propertyLookups)
                {
                    foreach (var v in dataBatch.Items)
                    {
                        plookup.Item2[GetValue(v, plookup.Item1)] = 1;
                    }
                }

                return Task.FromResult(true);
            }

            private string GetValue(T item, PropertyExtractor<T> property)
            {
                return property.GetValue(item)?.ToString() ?? string.Empty;
            }
        }
    }
}