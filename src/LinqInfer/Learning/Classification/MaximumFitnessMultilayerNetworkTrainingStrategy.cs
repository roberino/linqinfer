using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System.Diagnostics;

namespace LinqInfer.Learning.Classification
{
    internal class MaximumFitnessMultilayerNetworkTrainingStrategy<TClass, TInput> : IMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        private readonly NetworkParameterCache _paramCache;
        private readonly Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<TClass, NetworkParameters>, double> _fitnessFunction;
        private readonly Func<IClassifierTrainingContext<TClass, NetworkParameters>, int, TimeSpan, bool> _haltingFunction;

        public MaximumFitnessMultilayerNetworkTrainingStrategy(
            float errorTolerance = 0.3f,
            Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<TClass, NetworkParameters>, double> fitnessFunction = null,
            Func<IClassifierTrainingContext<TClass, NetworkParameters>, int, TimeSpan, bool> haltingFunction = null)
        {
            _paramCache = new NetworkParameterCache();
            _fitnessFunction = fitnessFunction ?? MultilayerNetworkFitnessFunctions.ErrorMinimisationFunction<TInput, TClass>();
            _haltingFunction = haltingFunction ?? ((c, i, t) => i > 1000);

            ErrorTolerance = errorTolerance;
            ParallelProcess = true;
        }

        public float ErrorTolerance { get; set; }

        public bool ParallelProcess { get; set; }

        public IClassifierTrainingContext<TClass, NetworkParameters> Train(IFeatureProcessingPipeline<TInput> featureSet, Func<NetworkParameters, IClassifierTrainingContext<TClass, NetworkParameters>> trainingContextFactory, Expression<Func<TInput, TClass>> classifyingExpression, ICategoricalOutputMapper<TClass> outputMapper)
        {
            const int hiddenLayerFactor = 8;

            var timer = new Stopwatch();

            var pipelineFact = new PipelineFactory(trainingContextFactory, featureSet.FeatureExtractor.VectorSize, outputMapper.VectorSize);

            var networks = Activators
                .All()
                .SelectMany(a => pipelineFact.GeneratePipelines(hiddenLayerFactor, a))
                .Concat(_paramCache.Get<TClass>().Take(2).Select(trainingContextFactory))
                .ToList();
            
            var classf = classifyingExpression.Compile();
            var nc = networks.Count;
            var i = 0;

            timer.Start();

            while (!_haltingFunction(networks.FirstOrDefault(), i, timer.Elapsed))
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

                nc = (int)(networks.Count * (2f / 3f)); // (iterationReductionFactor - 1) / iterationReductionFactor;

                if (networks.Count > 3) networks = networks.OrderByDescending(n => _fitnessFunction(featureSet.FeatureExtractor, n)).Take(nc).ToList();

                if (networks.All(n => HasConverged(n))) break;

                networks.Add(Breed(trainingContextFactory, networks[0], networks[1]));

                if (networks.Count > 2)
                    networks.Add(Breed(trainingContextFactory, networks[1], networks[2]));

                i++;
            }

            timer.Stop();

            var bestSolution = networks.OrderByDescending(n => _fitnessFunction(featureSet.FeatureExtractor, n)).First();

            DebugOutput.Log(bestSolution);

            _paramCache.Store<TClass>(bestSolution.Parameters, bestSolution.AverageError.GetValueOrDefault());

            return bestSolution;
        }

        protected virtual void OptimiseFeatures(IFeatureTransformBuilder<TInput> features, IEnumerable<IClassifierTrainingContext<TClass, NetworkParameters>> trainingSet)
        {
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
                var fact = new Func<int, IClassifierTrainingContext<TClass, NetworkParameters>>(n =>
                {
                    var parameters = new NetworkParameters(1, new int[] { _vectorSize, _vectorSize / 2 * n, _outputSize }, activator);

                    return _trainingContextFactory(parameters);
                });

                return Enumerable.Range(0, hiddenLayerFactor).Select(fact);
            }
        }
    }
}
