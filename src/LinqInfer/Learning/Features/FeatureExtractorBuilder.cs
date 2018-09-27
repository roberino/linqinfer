using LinqInfer.Data.Pipes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    class FeatureExtractorBuilder<T>
        where T : class
    {
        readonly Type _actualType;
        readonly IFeatureExtractionStrategy<T>[] _strategies;

        public FeatureExtractorBuilder(Type type, params IFeatureExtractionStrategy<T>[] strategies)
        {
            _strategies = strategies.Length == 0 ? new[] { new DefaultFeatureExtractionStrategy<T>() } : strategies;
            _actualType = type;

            Setup();
        }

        public async Task<IFloatingPointFeatureExtractor<T>> BuildAsync(
            IAsyncEnumerator<T> samples,
            CancellationToken cancellationToken)
        {
            var extractors = new List<IFloatingPointFeatureExtractor<T>>();

            var strategyBuilders = _strategies
                .Where(s => s.CanBuild)
                .Select(s => s.CreateBuilder())
                .ToArray();

            await samples
                .CreatePipe()
                .RegisterSinks(strategyBuilders)
                .RunAsync(cancellationToken);

            foreach (var builder in strategyBuilders)
            {
                extractors.Add(await builder.BuildAsync());
            }

            return new MultiStrategyFeatureExtractor<T>(extractors.ToArray());
        }

        void Setup()
        {
            var properties = ObjectFeatureExtractor<T>.GetFeatureProperties(_actualType);

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