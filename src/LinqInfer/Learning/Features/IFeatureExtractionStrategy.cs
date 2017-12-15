using System.Collections.Generic;
using System.Threading.Tasks;
using LinqInfer.Data.Pipes;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractionStrategy<T>
    {
        int Priority { get; set; }
        IList<PropertyExtractor<T>> Properties { get; }

        Task<IFloatingPointFeatureExtractor<T>> BuildAsync(IAsyncEnumerator<T> samples);
        bool CanHandle(PropertyExtractor<T> propertyExtractor);
    }
}