using LinqInfer.Learning;
using LinqInfer.Probability;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Storage
{
    public static class DataSampleExtensions
    {
        public static IDictionary<ColumnVector1D, double> CreateMultiVariateDistribution(this DataSample sample, int binCount = 10)
        {
            var kde = new KernelDensityEstimator();

            return kde.CreateMultiVariateDistribution(sample.SampleData.Select(d => d.AsColumnVector()).AsQueryable(), binCount);
        }

        public static IDictionary<ColumnVector1D, double> CreateHistogram(this DataSample sample, int binCount = 10)
        {
            var hist = new Histogram();

            return null; // hist.Analyse()
        }

        public static FeatureMap<DataItem> CreateSofm(this DataSample sample, int nodeCount = 10, float learningRate = 0.5f)
        {
            var maxSample = sample.SampleData.Select(d => d.AsColumnVector()).MaxOfEachDimension().ToSingleArray();
            var labels = sample.Metadata.Fields.Count > 0 ? sample.Metadata.Fields.Select(f => f.Label).ToArray() : null;

            var sofm = sample
                .SampleData
                .AsQueryable()
                .ToSofm(
                    x => x == null ? maxSample : x.AsColumnVector().ToSingleArray(), 
                    labels, nodeCount, learningRate);

            return sofm;
        }
    }
}