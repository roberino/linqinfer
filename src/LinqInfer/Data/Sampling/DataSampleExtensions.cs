using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Data.Sampling
{
    public static class DataSampleExtensions
    {
        /// <summary>
        /// Creates a processing pipeline for further analysis / processing of the data.
        /// </summary>
        /// <param name="sample">The sample data</param>
        /// <returns>A Feature processing pipeline</returns>
        public static FeatureProcessingPipeline<DataItem> CreatePipeline(this DataSample sample)
        {
            var featureExtractor = sample.CreateFeatureExtractor();
            var data = sample.SampleData.AsQueryable();

            return new FeatureProcessingPipeline<DataItem>(data, featureExtractor);
        }

        /// <summary>
        /// Returns a multi-variate distribution for a sample.
        /// </summary>
        /// <param name="sample">The sample data</param>
        /// <param name="binCount">The number of bins</param>
        /// <returns></returns>
        public static IDictionary<ColumnVector1D, double> CreateMultiVariateDistribution(this DataSample sample, int binCount = 10)
        {
            var kde = new KernelDensityEstimator();

            return kde.CreateMultiVariateDistribution(sample.SampleData.Select(d => d.AsColumnVector()).AsQueryable(), binCount);
        }
    }
}