using LinqInfer.Data.Pipes;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractionStrategy<T>
    {
        /// <summary>
        /// Specifies the priority in which the strategy will be used
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// Returns true if the strategy can build
        /// </summary>
        bool CanBuild { get; }

        /// <summary>
        /// List of properties to be consumed by the strategy
        /// </summary>
        IList<PropertyExtractor<T>> Properties { get; }

        /// <summary>
        /// Creates a builder for creating feature extractors
        /// </summary>
        IAsyncBuilderSink<T, IFloatingPointFeatureExtractor<T>> CreateBuilder();

        /// <summary>
        /// Returns true if the strategy
        /// can handle and use the property
        /// </summary>
        bool CanHandle(PropertyExtractor<T> propertyExtractor);
    }
}