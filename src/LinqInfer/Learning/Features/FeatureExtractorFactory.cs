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

    class FeatureExtractorFactory<T> : IFeatureExtractorFactory<T>
    {
        readonly IDictionary<string,
                Func<FeatureExtractorLoadContext<T>, IVectorFeatureExtractor<T>>>
            _featureExtractorFactories;

        public FeatureExtractorFactory(
            params (string type, Func<FeatureExtractorLoadContext<T>, IVectorFeatureExtractor<T>> fact)[] factories)
        {
            _featureExtractorFactories = factories.ToDictionary(f => f.type, f => f.fact);
        }

        public IFeatureExtractorFactory<T> Register<TF>(Func<FeatureExtractorLoadContext<T>, IVectorFeatureExtractor<T>> factoryMethod)
            where TF : IVectorFeatureExtractor<T>
        {
            _featureExtractorFactories[NameOf<TF>()] = factoryMethod;
            return this;
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
                    (NameOf<CategoricalFeatureExtractor<T, string>>(),
                        d => CategoricalFeatureExtractor<T, string>.Create(d.Data)),
                    (NameOf<FeatureMapDataExtractor<T>>(),
                        d => FeatureMapDataExtractor<T>.Create(d.Data, d.Factory.Create)));
            }
        }

        public IVectorFeatureExtractor<T> Create(PortableDataDocument doc)
        {
            try
            {
                var factory = _featureExtractorFactories[doc.TypeName];

                return factory(new FeatureExtractorLoadContext<T>(doc, this));
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"Type not found {doc.TypeName}");
            }
        }

        static string NameOf<TX>()
        {
            return typeof(TX).Name;
        }
    }
}