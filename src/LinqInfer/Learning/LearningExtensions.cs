using LinqInfer.Learning.Features;
using LinqInfer.Learning.Nn;
using LinqInfer.Probability;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    public static class LearningExtensions
    {
        private static readonly ObjectFeatureExtractor _ofo = new ObjectFeatureExtractor();

        public static IEnumerable<ClusterNode<T>> ToSofm<T>(this IQueryable<T> values, T normalisingSample = null, int outputNodeCount = 10, bool normaliseData = true, float learningRate = 0.5f) where T : class
        {
            var fm = new FeatureMap<T>(_ofo.CreateFeatureExtractor<T>(normaliseData), normalisingSample, outputNodeCount, learningRate);

            return fm.Map(values);
        }

        public static IEnumerable<ClusterNode<T>> ToSofm<T>(this IQueryable<T> values, Func<T, float[]> featureExtractor, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var fm = new FeatureMap<T>(featureExtractor, default(T), outputNodeCount, learningRate);

            return fm.Map(values);
        }

        public static Func<TInput, IDictionary<TClass, Fraction>> ToSimpleDistributionFunction<TInput, TClass>(this IQueryable<TInput> trainingData, Func<TInput, TClass> classf) where TInput : class
        {
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new SimpleNet<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            foreach (var batch in trainingData.Chunk())
            {
                classifierPipe.Train(batch.Select(v => new Tuple<TClass, TInput>(classf(v), v)));
            }

            return x =>
            {
                var matches = classifierPipe.FindPossibleMatches(x).ToList();
                var factor = Math.Max(matches.Count, 100);
                var total = (int)Math.Round(matches.Sum(m => m.Score * factor), 0);
                var dist = matches.ToDictionary(m => m.ClassType, m => new Fraction((int)Math.Round(m.Score * factor, 0), total));
                return dist;
            };
        }

        public static Func<TInput, ClassifyResult<TClass>> ToSimpleClassifier<TInput, TClass>(this IQueryable<TInput> trainingData, Func<TInput, TClass> classf) where TInput : class
        {
            var extractor = _ofo.CreateFeatureExtractor<TInput>();
            var net = new SimpleNet<TClass>(extractor.VectorSize);
            var classifierPipe = new ClassificationPipeline<TClass, TInput, float>(net, net, extractor);

            foreach (var batch in trainingData.Chunk())
            {
                classifierPipe.Train(batch.Select(v => new Tuple<TClass, TInput>(classf(v), v)));
            }

            return classifierPipe.Classify;
        }
    }
}
