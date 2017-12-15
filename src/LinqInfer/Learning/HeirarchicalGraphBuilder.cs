﻿using LinqInfer.Learning.Features;
using LinqInfer.Maths.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Learning
{
    internal sealed class HeirarchicalGraphBuilder<TInput> where TInput : class
    {
        private readonly IWeightedGraphStore<DataLabel<TInput>, double> _weightedGraphStore;
        private readonly ClusteringParameters _clusteringParameters;

        public HeirarchicalGraphBuilder(
            IWeightedGraphStore<DataLabel<TInput>, double> weightedGraphStore, int estimatedSampleSize)
        {
            _weightedGraphStore = weightedGraphStore;
            _clusteringParameters = new ClusteringParameters()
            {
                NumberOfOutputNodes = 2,
                EstimatedSampleSize = estimatedSampleSize
            };
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

        private async Task CreateBinaryGraph(IFloatingPointFeatureExtractor<TInput> featureExtractor, WeightedGraphNode<DataLabel<TInput>, double> parent, IEnumerable<TInput> input, CancellationToken cancellationToken)
        {
            if (input.Count() <= 2)
            {
                var vertexes = input.Select(i => parent.ConnectToAsync(new DataLabel<TInput>(i, i.ToString()), 1)).ToList();

                await Task.WhenAll(vertexes);

                return;
            }

            var sofm = new FeatureMapperV3<TInput>(_clusteringParameters);

            var asyncEnumerable = Data.Pipes.From.Enumerable(input);

            var pipeline = asyncEnumerable.CreatePipeine(featureExtractor);

            var map = await sofm.MapAsync(pipeline, cancellationToken);

            var tasks = map.Select(async n =>
            {
                var vertex = await parent.ConnectToAsync(CreateEmptyLabel(), 1);

                await CreateBinaryGraph(pipeline.FeatureExtractor, vertex, n, cancellationToken);
            }
            ).ToList();

            await Task.WhenAll(tasks);
        }

        private DataLabel<TInput> CreateEmptyLabel()
        {
            return new DataLabel<TInput>(Guid.NewGuid().ToString());
        }
    }
}