using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    sealed class HeirarchicalGraphBuilder<TInput> where TInput : class
    {
        readonly IWeightedGraphStore<DataLabel<TInput>, double> _weightedGraphStore;
        readonly ClusteringParameters _clusteringParameters;
        readonly Func<int, double> _weightFunction;

        public HeirarchicalGraphBuilder(
            IWeightedGraphStore<DataLabel<TInput>, double> weightedGraphStore, 
            Func<int, double> weightFunction = null)
        {
            _weightedGraphStore = weightedGraphStore;
            _clusteringParameters = new ClusteringParameters()
            {
                NumberOfOutputNodes = 2,
                TrainingEpochs = 100,
                InitialRadius = 0.4
            };
            _weightFunction = weightFunction ?? (n => n);
        }

        public async Task<WeightedGraph<DataLabel<TInput>, double>> CreateBinaryGraph(IAsyncFeatureProcessingPipeline<TInput> pipeline, CancellationToken cancellationToken)
        {
            var graph = new WeightedGraph<DataLabel<TInput>, double>(_weightedGraphStore, (x, y) => x + y);

            var sofm = new FeatureMapperV3<TInput>(_clusteringParameters);

            var map = await sofm.MapAsync(pipeline, cancellationToken);

            var root = await graph.FindOrCreateVertexAsync(new DataLabel<TInput>("Root"));

            var tasks = map.Select(n => CreateBinaryGraph(pipeline.FeatureExtractor, root, n, cancellationToken)).ToList();

            await Task.WhenAll(tasks);

            await graph.SaveAsync();

            return graph;
        }

        async Task CreateBinaryGraph(IFloatingPointFeatureExtractor<TInput> featureExtractor, WeightedGraphNode<DataLabel<TInput>, double> parent, IEnumerable<TInput> input, CancellationToken cancellationToken)
        {
            DebugOutput.Log($"Map nodes: {input.Count()}");

            if (input.Count() <= 2)
            {
                var vertexes = input.Select(i => parent.ConnectToAsync(new DataLabel<TInput>(i, i.ToString()), _weightFunction(1))).ToList();

                await Task.WhenAll(vertexes);

                return;
            }

            var sofm = new FeatureMapperV3<TInput>(_clusteringParameters);

            var asyncEnumerable = Data.Pipes.From.Enumerable(input);

            var pipeline = asyncEnumerable.CreatePipeline(featureExtractor);

            var map = await sofm.MapAsync(pipeline, cancellationToken);

            if (map.Count() == 1)
            {
                _clusteringParameters.InitialRadius *= 0.6;

                DebugOutput.Log($"Reduce radius: {_clusteringParameters.InitialRadius}");
            }

            var tasks = map.Select(async n =>
            {
                var vertex = await parent.ConnectToAsync(CreateEmptyLabel(), _weightFunction(n.Count()));

                await CreateBinaryGraph(pipeline.FeatureExtractor, vertex, n, cancellationToken);
            }
            ).ToList();

            await Task.WhenAll(tasks);
        }

        DataLabel<TInput> CreateEmptyLabel()
        {
            return new DataLabel<TInput>(Guid.NewGuid().ToString());
        }
    }
}