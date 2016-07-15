using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    internal class FeatureMapperV2<T> where T : class
    {
        private const int BATCH_SIZE = 1000;
        private readonly int _maxParallel;
        private readonly int _outputNodeCount;
        private readonly float _learningRate;
        private readonly Func<int, double> _initialiser;

        public FeatureMapperV2(int outputNodeCount = 10, float learningRate = 0.5f, bool parallel = false, Func<int, double> initialiser = null)
        {
            Contract.Assert(outputNodeCount > 0);
            Contract.Assert(learningRate > 0);

            _outputNodeCount = outputNodeCount;
            _learningRate = learningRate;
            _maxParallel = parallel ? Environment.ProcessorCount : 1;
            _initialiser = initialiser;
        }

        public FeatureMap<T> Map(IFeatureProcessingPipeline<T> pipeline)
        {
            HashSet<ClusterNode<T>> outputNodes = SetupOutputNodes(pipeline);

            foreach (var batch in pipeline.ExtractBatches())
            {
                batch.AsParallel().WithDegreeOfParallelism(_maxParallel).ForAll(v =>
                {
                    var bestMatch = outputNodes.OrderBy(c => c.CalculateDifference(v)).FirstOrDefault();
                    bestMatch.AppendMember(v);
                });
            }

            return new FeatureMap<T>(outputNodes.Where(n => n.IsInitialised), pipeline.FeatureMetadata);
        }

        protected HashSet<ClusterNode<T>> SetupOutputNodes(IFeatureProcessingPipeline<T> pipeline)
        {
            pipeline.NormaliseData();

            var dist = Functions.PercentileRange(_outputNodeCount);

            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(0, _outputNodeCount)
                        .Select(n =>
                            new ClusterNode<T>(pipeline.FeatureExtractor, CreateInitialVector((float)dist[n], pipeline.VectorSize), _learningRate)));
        }

        protected double[] CreateInitialVector(float weight, int length)
        {
            return Enumerable
                    .Range(0, length)
                    .Select(x => _initialiser == null ? weight : _initialiser(x))
                    .ToArray();
        }
    }
}