using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning.Features
{
    public static class FeatureTransformations
    {
        internal static double[] GroupedIntoAverages(double[] featureVector, int groupSize = 3)
        {
            int i = 0;
            return featureVector.GroupBy(f => (i++ % groupSize)).Select(g => g.Average()).ToArray();
        }

        /// <summary>
        /// Reduces the feature set by removing any features where all values for that feature within the pipeline
        /// are less than the specified threshold. This function pre-normalises the data so that the threshold should be between 0 and 1.
        /// </summary>
        /// <typeparam name="T">The input type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="threshold">A threshold between 0 and 1 (inclusive)</param>
        public static void ReduceFeaturesByThreshold<T>(this IFeatureProcessingPipeline<T> pipeline, float threshold = 0.5f) where T : class
        {
            Contract.Assert(threshold >= 0 && threshold <= 1);

            var f = ReduceByThresholdFunction(pipeline.NormaliseData(), threshold);

            pipeline.PreprocessWith(f);
        }

        private static Func<double[], double[]> ReduceByThresholdFunction(IFeatureDataSource dataSource, float threshold = 0.5f)
        {
            var toBeRemoved = Enumerable.Range(0, dataSource.VectorSize).ToDictionary(n => n, _ => true);

            foreach (var item in dataSource.ExtractVectors())
            {
                foreach(var x in toBeRemoved.Where(k => k.Value).ToList())
                {
                    if (item[x.Key] >= threshold)
                    {
                        toBeRemoved[x.Key] = false;
                    }
                }
            }

            var indexes = toBeRemoved.Where(x => !x.Value).Select(x => x.Key).ToList();

            if (indexes.Count == dataSource.VectorSize) return m => m;

            return m => indexes.Select(i => m[i]).ToArray();
        } 
    }
}