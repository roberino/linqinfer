using LinqInfer.Learning.Features;
using LinqInfer.Math;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class FeatureMapper<T>
    {
        private const int BATCH_SIZE = 1000;
        private readonly IFloatingPointFeatureExtractor<T> _featureExtractor;
        private readonly HashSet<ClusterNode<T>> _outputNodes;
        private readonly int _maxParallel;

        public FeatureMapper(Func<T, float[]> featureExtractor, T initialSample, int outputNodeCount = 10, float learningRate = 0.5f, bool parallel = false)
        {
            _featureExtractor = new DelegatingFloatingPointFeatureExtractor<T>(featureExtractor, featureExtractor(initialSample).Length, false);
            _outputNodes = SetupOutputNodes(initialSample, outputNodeCount, learningRate);
            _maxParallel = parallel ? 16 : 1;
        }

        public FeatureMapper(IFloatingPointFeatureExtractor<T> featureExtractor, T initialSample, int outputNodeCount = 10, float learningRate = 0.5f, bool parallel = false)
        {
            _featureExtractor = featureExtractor;
            _outputNodes = SetupOutputNodes(initialSample, outputNodeCount, learningRate);
            _maxParallel = parallel ? 16 : 1;
        }

        public IDictionary<string, int> FeatureLabels
        {
            get
            {
                return _featureExtractor.Labels;
            }
        }

        public FeatureMap<T> Map(IQueryable<T> values)
        {
            int next = 0;

            while (true)
            {
                var batch = values.Skip(next).Take(BATCH_SIZE).ToList();

                batch.AsParallel().WithDegreeOfParallelism(_maxParallel).ForAll(Process);

                next += BATCH_SIZE;

                if (batch.Count < BATCH_SIZE) break;
            }

            return new FeatureMap<T>(_outputNodes.Where(n => n.IsInitialised), _featureExtractor.Labels);
        }

        private void Process(T value)
        {
            var v = new ObjectVector<T>(value, _featureExtractor.ExtractVector(value));
            var bestMatch = _outputNodes.OrderBy(c => c.CalculateDifference(v)).FirstOrDefault();
            bestMatch.AppendMember(v);
        }

        protected HashSet<ClusterNode<T>> SetupOutputNodes(T initialSample, int outputNodeCount = 10, float learningRate = 0.5f)
        {
            var sampleVector = _featureExtractor.CreateNormalisingVector(initialSample);
            var vectorSize = sampleVector.Length;
            var dist = Functions.PercentileRange(outputNodeCount);

            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(0, outputNodeCount)
                        .Select(n =>
                            new ClusterNode<T>(_featureExtractor, CreateInitialVector(sampleVector, (float)dist[n]), learningRate)));
        }

        protected float[] CreateInitialVector(float[] sampleVector, float weight)
        {
            return Enumerable
                        .Range(0, sampleVector.Length)
                        .Select(x => weight * sampleVector[x])
                        .ToArray();
        }
    }
}