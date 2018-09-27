using LinqInfer.Data.Serialisation;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Features
{
    interface IFeatureExtractorFactory<T> : IFactory<IFloatingPointFeatureExtractor<T>, PortableDataDocument>
    {
    }
}