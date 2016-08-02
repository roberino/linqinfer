using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Learning
{
    /// <summary>
    /// See  https://en.wikipedia.org/wiki/K-means_clustering and https://en.wikipedia.org/wiki/Self-organizing_map
    /// </summary>
    internal class FeatureMapperV2<T> where T : class
    {
        private const int BATCH_SIZE = 1000;
        private readonly int _outputNodeCount;
        private readonly float _learningRate;
        private readonly Func<int, ColumnVector1D> _initialiser;
        private readonly float? _radius;

        public FeatureMapperV2(int outputNodeCount = 10, float learningRate = 0.5f, float? radius = null, Func<int, ColumnVector1D> initialiser = null)
        {
            Contract.Assert(outputNodeCount > 0);
            Contract.Assert(learningRate > 0);
            Contract.Assert(!_radius.HasValue || (_radius.Value > 0 && _radius.Value < 1));

            _outputNodeCount = outputNodeCount;
            _learningRate = learningRate;
            _initialiser = initialiser;
            _radius = radius;
        }

        public FeatureMap<T> Map(IFeatureProcessingPipeline<T> pipeline)
        {
            HashSet<ClusterNode<T>> outputNodes = SetupOutputNodes(pipeline);

            int i = 0;
            var iterationsMax = pipeline.Data.Count();

            foreach (var batch in pipeline.ExtractBatches())
            {
                batch
                    .RandomOrder()       
                    .AsParallel()
                    .WithDegreeOfParallelism(1).ForAll(v =>
                {
                    var bestMatch = outputNodes.OrderBy(c => c.CalculateDifference(v)).FirstOrDefault();

                    bestMatch.AdjustForIteration(outputNodes, v, i, iterationsMax);

                    i++;
                });
            }

            return new FeatureMap<T>(outputNodes.Where(n => n.IsInitialised), pipeline.FeatureExtractor);
        }

        protected virtual HashSet<ClusterNode<T>> SetupOutputNodes(IFeatureProcessingPipeline<T> pipeline)
        {
            pipeline.NormaliseData();

            var dist = Functions.PercentileRange(_outputNodeCount);

            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(0, _outputNodeCount)
                        .Select(n =>
                            new ClusterNode<T>(pipeline.FeatureExtractor, CreateInitialVector((float)dist[n], n, pipeline.VectorSize), _learningRate, _radius)));
        }

        protected double[] CreateInitialVector(float weight, int n, int length)
        {
            if (_initialiser == null)
            {
                return Enumerable
                        .Range(0, length)
                        .Select(x => (double)weight)
                        .ToArray();
            }
            else
            {
                return _initialiser(n).GetUnderlyingArray();
            }
        }
    }
}