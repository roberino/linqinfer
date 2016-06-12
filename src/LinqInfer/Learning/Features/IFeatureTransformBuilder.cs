using System;
using LinqInfer.Data;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureTransformBuilder<T> where T : class
    {
        IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }
        IEnumerable<IBlobStore> Outputs { get; }
        FeatureProcessingPipline<T> FilterFeatures(Func<IFeature, bool> featureFilter);
        FeatureProcessingPipline<T> FilterFeaturesByProperty(Action<PropertySelector<T>> selector);
        FeatureProcessingPipline<T> PreprocessWith(Func<double[], double[]> transformFunction);
        FeatureProcessingPipline<T> OutputResultsTo(IBlobStore store);
    }
}