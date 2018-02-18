using LinqInfer.Data.Pipes;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public abstract class FeatureExtractionStrategy<T> : IFeatureExtractionStrategy<T>
    {
        protected FeatureExtractionStrategy()
        {
            Properties = new List<PropertyExtractor<T>>();
        }

        public int Priority { get; set; } = 1;
        
        public IList<PropertyExtractor<T>> Properties { get; }

        public virtual bool CanBuild => Properties.Any();

        public virtual bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return true;
        }

        public abstract IAsyncBuilderSink<T, IFloatingPointFeatureExtractor<T>> CreateBuilder();
    }
}