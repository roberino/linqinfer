using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Learning
{
    /// <summary>
    /// See  https://en.wikipedia.org/wiki/K-means_clustering and https://en.wikipedia.org/wiki/Self-organizing_map
    /// </summary>
    internal class FeatureMapperV3<T> where T : class
    {
        private const int BATCH_SIZE = 1000;
        private readonly ClusteringParameters _parameters;

        public FeatureMapperV3(int outputNodeCount = 10, float learningRate = 0.5f, int trainingEpochs = 1000, float? radius = null)
        {
            _parameters = new ClusteringParameters()
            {
                InitialLearningRate = learningRate,
                InitialRadius = radius,
                NumberOfOutputNodes = outputNodeCount,
                TrainingEpochs = trainingEpochs
            };

            _parameters.Validate();
        }

        public FeatureMapperV3(ClusteringParameters parameters)
        {
            _parameters = parameters;
            _parameters.Validate();
        }

        public FeatureMap<T> Map(IFeatureProcessingPipeline<T> pipeline)
        {
            HashSet<ClusterNode<T>> outputNodes = SetupOutputNodes(pipeline);

            var iterationsMax = pipeline.SampleCount * _parameters.TrainingEpochs;

            for (int i = 0; i < _parameters.TrainingEpochs; i++)
            {
                Run(pipeline, outputNodes, i, iterationsMax, i == _parameters.TrainingEpochs - 1);
            }

            return new FeatureMap<T>(outputNodes.Where(n => n.IsInitialised).ToList(), pipeline.FeatureExtractor, _parameters);
        }

        private void Run(IFeatureProcessingPipeline<T> pipeline, HashSet<ClusterNode<T>> outputNodes, int iteration, int iterationsMax, bool append)
        {
            foreach (var batch in pipeline.ExtractBatches())
            {
                batch
                    .RandomOrder()
                    .AsParallel()
                    .WithDegreeOfParallelism(1).ForAll(v =>
                    {
                        var bestMatch = outputNodes.OrderBy(n => n.CalculateDifference(v)).FirstOrDefault();

                        bestMatch.AdjustForIteration(outputNodes, v, iteration, append);
                    });
            }
        }

        protected virtual HashSet<ClusterNode<T>> SetupOutputNodes(IFeatureProcessingPipeline<T> pipeline)
        {
            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(0, _parameters.NumberOfOutputNodes)
                        .Select(n =>
                            new ClusterNode<T>(pipeline.FeatureExtractor, _parameters.WeightInitialiser(n, pipeline.FeatureExtractor.VectorSize), _parameters)));
        }
    }
}