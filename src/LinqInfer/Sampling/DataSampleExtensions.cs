using LinqInfer.Learning;
using LinqInfer.Learning.Classification;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Sampling
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

        public static Func<DataItem, ClassifyResult<string>> CreateClassifier(this DataSample sample, int[] selectedFeatures = null)
        {
            var featureExtractor = sample.CreateFeatureExtractor(selectedFeatures);

            var clsf = sample
                .SampleData
                .AsQueryable()
                .ToMultilayerNetworkClassifier(x => x.Label);

            return x => clsf(x).FirstOrDefault();
        }

        public static FeatureMap<DataItem> CreateSofm(this DataSample sample, int nodeCount = 10, float learningRate = 0.5f, int[] selectedFeatures = null)
        {
            var featureExtractor = sample.CreateFeatureExtractor(selectedFeatures);

            var sofm = sample
                .SampleData
                .AsQueryable()
                .ToSofm(featureExtractor, nodeCount, learningRate);

            return sofm;
        }
    }
}