using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;

namespace LinqInfer.Learning.Classification
{
    internal class MinErrorMultilayerNetworkTrainingStrategy<TClass, TInput> : IMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        private readonly int _maxIterations;
        private readonly NetworkParameterCache _paramCache;

        public MinErrorMultilayerNetworkTrainingStrategy(
            float errorTolerance = 0.3f,
            int maxIterations = 200)
        {
            _maxIterations = maxIterations;
            _paramCache = new NetworkParameterCache();

            ErrorTolerance = errorTolerance;
            ParallelProcess = false;
        }

        public float ErrorTolerance { get; set; }

        public bool ParallelProcess { get; set; }

        public IClassifierTrainingContext<TClass, NetworkParameters> Train(IFeatureProcessingPipeline<TInput> featureSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, Expression<Func<TInput, TClass>> classifyingExpression, ICategoricalOutputMapper<TClass> outputMapper)
        {
            const int hiddenLayerFactor = 8;

            var pipelineFact = new PipelineFactory(trainingContextFactory, featureSet.FeatureExtractor.VectorSize, outputMapper.VectorSize);

            var networks = Activators
                .All()
                .SelectMany(a => pipelineFact.GeneratePipelines(hiddenLayerFactor, a))
                .Concat(_paramCache.Get<TClass>().Take(2).Select(trainingContextFactory))
                .ToList();

            var iterationReductionFactor = hiddenLayerFactor; // reduce by 1/iterationReductionFactor each iteration
            var classf = classifyingExpression.Compile();
            var nc = networks.Count;
            var i = 0;

            while (i < _maxIterations)
            {
                var unconverged = (i == 0) ? networks : networks.Where(n => !HasConverged(n)).ToList();

                unconverged.AsParallel().ForAll(n => n.ResetError());

                foreach (var batch in featureSet.ExtractBatches())
                {
                    unconverged.AsParallel().WithDegreeOfParallelism(ParallelProcess ? Environment.ProcessorCount : 1).ForAll(n =>
                    {
                        foreach (var value in batch)
                        {
                            n.Train(classf(value.Value), value.Vector);
                        }
                    });
                }

                nc = networks.Count * (iterationReductionFactor - 1) / iterationReductionFactor;

                if (networks.Count > 3) networks = networks.OrderBy(n => n.CumulativeError).Take(nc).ToList();

                if (networks.All(n => HasConverged(n))) break;

                networks.Add(Breed(trainingContextFactory, networks[0], networks[1]));

                i++;
            }


            var bestSolution = networks.OrderBy(n => n.CumulativeError).First();

            Debugger.Log(bestSolution);

            _paramCache.Store<TClass>(bestSolution.Parameters, bestSolution.AverageError.GetValueOrDefault());

            return bestSolution;
        }

        private IClassifierTrainingContext<TClass, NetworkParameters> Breed(Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, IClassifierTrainingContext<TClass, NetworkParameters> contextA, IClassifierTrainingContext<TClass, NetworkParameters> contextB)
        {
            var newParameters = contextA.Parameters.Breed(contextB.Parameters);

            return trainingContextFactory(newParameters);
        }

        private bool HasConverged(IClassifierTrainingContext<TClass, NetworkParameters> context)
        {
            return context.AverageError.HasValue && context.AverageError <= ErrorTolerance;
        }

        private class PipelineFactory
        {
            private readonly Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> _trainingContextFactory;
            private readonly int _vectorSize;
            private readonly int _outputSize;

            public PipelineFactory(Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, int vectorSize, int outputSize)
            {
                _trainingContextFactory = trainingContextFactory;
                _vectorSize = vectorSize;
                _outputSize = outputSize;
            }

            public IEnumerable<IClassifierTrainingContext<TClass, NetworkParameters>> GeneratePipelines(int hiddenLayerFactor, ActivatorFunc activator)
            {
                return Enumerable.Range(0, hiddenLayerFactor)
                    .Select(n =>
                    {
                        var parameters = new NetworkParameters(1, new int[] { _vectorSize, _vectorSize / 2 * n, _outputSize }, activator);

                        return _trainingContextFactory(parameters);
                    });
            }
        }
    }
}
