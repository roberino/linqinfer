using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    /// <summary>
    /// See  https://en.wikipedia.org/wiki/K-means_clustering and https://en.wikipedia.org/wiki/Self-organizing_map
    /// </summary>
    class FeatureMapperV3<T> where T : class
    {
        readonly ClusteringParameters _parameters;

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

        public Task<FeatureMap<T>> MapAsync(IAsyncFeatureProcessingPipeline<T> pipeline, CancellationToken cancellationToken)
        {
            HashSet<ClusterNode<T>> outputNodes = SetupOutputNodes(pipeline.FeatureExtractor);

            return MapAsync(outputNodes, pipeline, cancellationToken);
        }

        public FeatureMap<T> Map(IFeatureProcessingPipeline<T> pipeline)
        {
            HashSet<ClusterNode<T>> outputNodes = SetupOutputNodes(pipeline.FeatureExtractor);
            
            for (int i = 0; i < _parameters.TrainingEpochs; i++)
            {
                Run(pipeline, outputNodes, i, i == _parameters.TrainingEpochs - 1);
            }

            return new FeatureMap<T>(outputNodes.Where(n => n.IsInitialised).ToList(), pipeline.FeatureExtractor, _parameters);
        }

        public async Task<FeatureMap<T>> MapAsync(HashSet<ClusterNode<T>> outputNodes, IAsyncFeatureProcessingPipeline<T> pipeline, CancellationToken cancellationToken)
        {
            for (int i = 0; i < _parameters.TrainingEpochs; i++)
            {
                await RunAsync(pipeline, outputNodes, i, i == _parameters.TrainingEpochs - 1, cancellationToken);
            }

            return new FeatureMap<T>(outputNodes.Where(n => n.IsInitialised).ToList(), pipeline.FeatureExtractor, _parameters);
        }

        async Task RunAsync(IAsyncFeatureProcessingPipeline<T> pipeline, HashSet<ClusterNode<T>> outputNodes, int iteration, bool append, CancellationToken cancellationToken)
        {
            await pipeline
                   .ExtractBatches()
                   .ProcessUsing(b =>
                   {
                       foreach (var v in b.Items)
                       {
                           if (cancellationToken.IsCancellationRequested) return;

                           var bestMatch = outputNodes.OrderBy(n => n.CalculateDifference(v)).FirstOrDefault();

                           bestMatch.AdjustForIteration(outputNodes, v, iteration, append);
                       }
                   }, cancellationToken);
        }

        void Run(IFeatureProcessingPipeline<T> pipeline, HashSet<ClusterNode<T>> outputNodes, int iteration, bool append)
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

        internal HashSet<ClusterNode<T>> SetupOutputNodes(IFloatingPointFeatureExtractor<T> featureExtractor)
        {
            return new HashSet<ClusterNode<T>>(
                    Enumerable
                        .Range(0, _parameters.NumberOfOutputNodes)
                        .Select(n =>
                            new ClusterNode<T>(featureExtractor, _parameters.WeightInitialiser(n, featureExtractor.VectorSize), _parameters)));
        }
    }
}