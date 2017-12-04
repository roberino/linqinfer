using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqInfer.Learning.Features
{
    public class FeatureExtractorBuilder<T>
        where T : class
    {
        private readonly Type _actualType;
        private readonly FeatureExtractionStrategy<T>[] _strategies;

        public FeatureExtractorBuilder(Type type, params FeatureExtractionStrategy<T>[] strategies)
        {
            _strategies = strategies.Length == 0 ? new[] { new DefaultFeatureExtractionStrategy<T>() } : strategies;
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