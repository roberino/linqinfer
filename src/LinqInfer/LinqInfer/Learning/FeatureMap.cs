using LinqInfer.Learning.Features;
using LinqInfer.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class FeatureMap<T>
    {
        private const int BATCH_SIZE = 1000;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly HashSet<ClusterNode<T>> _outputNodes;
        private readonly int _maxParallel;

        public FeatureMap(Func<T, float[]> featureExtractor, T initialSample, int outputNodeCount = 10, float learningRate = 0.5f, bool parallel = false)
        {
            _featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(featureExtractor, featureExtractor(initialSample).Length, false);
            _outputNodes = SetupOutputNodes(initialSample, outputNodeCount, learningRate);
            _maxParallel = parallel ? 16 : 1;
        }

        public FeatureMap(IFloatingPointFeatureExtractor<T> featureExtractor, T initialSample, int outputNodeCount = 10, float learningRate = 0.5f, bool parallel = false)
        {
            _featureExtractor = featureExtractor;
            _outputNodes = SetupOutputNodes(initialSample, outputNodeCount, learningRate);
            _maxParallel = parallel ? 16 : 1;
        }

        public IEnumerable<ClusterNode<T>> Map(IQueryable<T> values)
        {
            int next = 0;

            while (true)
            {
                var batch = values.Skip(next).Take(BATCH_SIZE).ToList();

                batch.AsParallel().WithDegreeOfParallelism(_maxParallel).ForAll(Process);

                next += BATCH_SIZE;

                if (batch.Count < BATCH_SIZE) break;
            }

            return _outputNodes.Where(n => n.IsInitialised);
        }

        private void Process(T value)
        {
            var v = new ObjectVector<T>(value, _featureExtractor.ExtractVector(value));
            var bestMatch = _outputNodes.OrderBy(c => c.CalculateDifference(v)).FirstOrDefault();
            bestMatch.AdjustAndAppend(v);
        }

        protected HashSet<ClusterNode<T>> SetupOutputNodes(T initialSample, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var sampleVector = _featureExtractor.CreateNormalisingVector(initialSample);
            var vectorSize = sampleVector.Length;
            var rnd = new Random(DateTime.Now.Millisecond);
            var maxVectorValue = sampleVector.Max();

            if (maxVectorValue == 0) maxVectorValue = 1f;

            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(1, outputNodeCount)
                        .Select(n =>
                            new ClusterNode<T>(CreateInitialVector(n, vectorSize, sampleVector), learningRate)));
        }

        protected float[] CreateInitialVector(int n, int size, float[] sampleVector)
        {
            var dist = Functions.PercentileRange(size);

            return Enumerable
                        .Range(0, size)
                        .Select(x => (float)dist[n] * sampleVector[n])
                        .ToArray();
        }
    }
}