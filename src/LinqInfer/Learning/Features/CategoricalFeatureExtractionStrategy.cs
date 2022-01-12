using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Utility.Expressions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    class CategoricalFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
        where T : class
    {
        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return base.CanHandle(propertyExtractor) && propertyExtractor.FeatureMetadata.Model == FeatureVectorModel.Categorical;
        }

        public override IAsyncBuilderSink<T, IVectorFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder();
        }

        class Builder : IAsyncBuilderSink<T, IVectorFeatureExtractor<T>>
        {
            readonly TypeMapper<T> _typeMapper;
            readonly IDictionary<string, IDictionary<string, bool>> _lookup;

            public Builder()
            {
                _typeMapper = new TypeMapper<T>(1024, 
                    p => p.FeatureMetadata.Model == FeatureVectorModel.Categorical);

                _lookup = new Dictionary<string, IDictionary<string, bool>>();
            }

            public bool CanReceive => true;

            public Task<IVectorFeatureExtractor<T>> BuildAsync()
            {
                var extractors = new List<IVectorFeatureExtractor<T>>(_lookup.Count);

                foreach (var valueSet in _lookup)
                {
                    var set = new HashSet<string>(valueSet.Value.Keys);

                    var exp = $"x => ToString(Property(x, {valueSet.Key}, empty))".AsExpression<T, string>();

                    var fe = new CategoricalFeatureExtractor<T, string>(exp, set);

                    extractors.Add(fe);
                }

                return Task.FromResult<IVectorFeatureExtractor<T>>(new MultiStrategyFeatureExtractor<T>(extractors.ToArray()));
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                foreach (var v in dataBatch.Items)
                {
                    var map = _typeMapper.GetOrCreateMap(v.GetType());

                    foreach (var prop in map.Properties)
                    {
                        if (!_lookup.TryGetValue(prop.Property.Name, out var vals))
                        {
                            _lookup[prop.Property.Name] = vals = new Dictionary<string, bool>();
                        }

                        vals[GetValue(v, prop)] = true;
                    }
                }

                return Task.FromResult(true);
            }

            static string GetValue(T item, PropertyExtractor<T> property)
            {
                return property.GetValue(item)?.ToString() ?? string.Empty;
            }
        }
    }
}