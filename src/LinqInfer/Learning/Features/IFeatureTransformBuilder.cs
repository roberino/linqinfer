using LinqInfer.Maths;
using System;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureTransformBuilder<T> where T : class
    {
        /// <summary>
        /// Returns the feature extractor
        /// </summary>
        IFloatingPointFeatureExtractor<T> FeatureExtractor { get; }
        
        /// <summary>
        /// Centres the features around the mean
        /// </summary>
        IFeatureProcessingPipeline<T> CentreFeatures();

        /// <summary>
        /// Transforms the data so that it fits between
        /// the given range
        /// </summary>
        /// <param name="range">The range (defaults to -1 <= =< 1)</param>
        /// <returns></returns>
        IFeatureProcessingPipeline<T> ScaleFeatures(Range? range = null);

        /// <summary>
        /// Performs simple normalisation over the data,
        /// readjusting data so that values fall between 0 and 1
        /// </summary>
        IFeatureProcessingPipeline<T> NormaliseData();
    }
}