using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    internal class FeatureExtractorBuilder<T>
        where T : class
    {
        private readonly Type _actualType;
        private readonly IFeatureExtractionStrategy<T>[] _strategies;

        public FeatureExtractorBuilder(Type type, params IFeatureExtractionStrategy<T>[] strategies)
        {
            _strategies = strategies.Length == 0 ? new[] { new DefaultFeatureExtractionStrategy<T>() } : strategies;
            _actualType = type;

            Setup();
        }

        public async Task<IFloatingPointFeatureExtractor<T>> BuildAsync(IAsyncEnumerator<T> samples)
        {
            var extractors = new List<IFloatingPointFeatureExtractor<T>>();

            foreach (var strategy in _strategies.Where(s => s.Properties.Any()))
            {
                extractors.Add(await strategy.BuildAsync(samples));
            }

            return new MultiStrategyFeatureExtractor<T>(extractors.ToArray());
        }

        private void Setup()
        {
            var properties = new ObjectFeatureExtractorFactory().GetFeatureProperties<T>(_actualType);

            foreach (var prop in properties)
            {
                var strategy = _strategies
                    .Where(s => s.CanHandle(prop))
                    .OrderByDescending(s => s.Priority)
                    .FirstOrDefault();

                if (strategy != null)
                {
                    strategy.Properties.Add(prop);
                }
            }
        }
    }
}