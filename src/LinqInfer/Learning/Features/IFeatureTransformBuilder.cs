using System;
using LinqInfer.Data;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureTransformBuilder<T> where T : class
    {
        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }

        /// <summary>
        /// Returns storage outputs
        /// </summary>
        IEnumerable<IBlobStore> Outputs { get; }

        /// <summary>
        /// Filters features based on a predicate function
        /// </summary>
        FeatureProcessingPipline<T> FilterFeatures(Func<IFeature, bool> featureFilter);

        /// <summary>
        /// Filters features based on their mapped property name (assumes a direct mapping between property name and feature key)
        /// </summary>
        FeatureProcessingPipline<T> FilterFeaturesByProperty(Action<PropertySelector<T>> selector);

        /// <summary>
        /// Applies a pre-processing function which transforms the source vector into a new vector
        /// </summary>
        FeatureProcessingPipline<T> PreprocessWith(Func<double[], double[]> transformFunction);

        /// <summary>
        /// Specifies a storage output for persisting data
        /// </summary>
        FeatureProcessingPipline<T> OutputResultsTo(IBlobStore store);
    }
}