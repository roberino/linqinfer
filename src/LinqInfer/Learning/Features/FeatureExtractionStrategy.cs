using LinqInfer.Data.Pipes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Features
{
    public abstract class FeatureExtractionStrategy<T> : IFeatureExtractionStrategy<T>
    {
        protected FeatureExtractionStrategy()
        {
            Properties = new List<PropertyExtractor<T>>();
        }

        public int Priority { get; set; } = 1;

        public virtual bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return true;
        }

        public IList<PropertyExtractor<T>> Properties { get; }

        public abstract Task<IFloatingPointFeatureExtractor<T>> BuildAsync(IAsyncEnumerator<T> samples);
    }
}