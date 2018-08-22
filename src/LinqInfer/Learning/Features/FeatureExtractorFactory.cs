using LinqInfer.Data.Serialisation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    class FeatureExtractorLoadContext<T>
    {
        public FeatureExtractorLoadContext(PortableDataDocument data, IFeatureExtractorFactory<T> factory)
        {
            Data = data;
            Factory = factory;
        }

        public PortableDataDocument Data { get; }
        public IFeatureExtractorFactory<T> Factory { get; }
    }

    class FeatureExtractorFactory<T> : IFeatureExtractorFactory<T> where T : class
    {
        readonly IDictionary<string,
                Func<FeatureExtractorLoadContext<T>, IFloatingPointFeatureExtractor<T>>>
            _featureExtractorFactories;

        public FeatureExtractorFactory(
            params (string type, Func<FeatureExtractorLoadContext<T>, IFloatingPointFeatureExtractor<T>> fact)[] factories)
        {
            _featureExtractorFactories = factories.ToDictionary(f => f.type, f => f.fact);
        }

        public static FeatureExtractorFactory<T> Default
        {
            get
            {
                return new FeatureExtractorFactory<T>(
                    (NameOf<TransformingFeatureExtractor<T>>(),
                        d => TransformingFeatureExtractor<T>.Create(d.Data, d.Factory.Create)),
                    (NameOf<ExpressionFeatureExtractor<T>>(),
                        d => ExpressionFeatureExtractor<T>.Create(d.Data)),
                    (NameOf<MultiStrategyFeatureExtractor<T>>(),
                        d => MultiStrategyFeatureExtractor<T>.Create(d.Data, d.Factory.Create)),
                    (NameOf<ObjectFeatureExtractor<T>>(),
                        d => ObjectFeatureExtractor<T>.Create(d.Data)),
                    (NameOf<FeatureMapDataExtractor<T>>(),
                        d => FeatureMapDataExtractor<T>.Create(d.Data, d.Factory.Create)));
            }
        }

        public IFloatingPointFeatureExtractor<T> Create(PortableDataDocument doc)
        {
            var factory = _featureExtractorFactories[doc.TypeName];

            return factory(new FeatureExtractorLoadContext<T>(doc, this));
        }

        static string NameOf<TX>()
        {
            return typeof(TX).Name;
        }
    }
}