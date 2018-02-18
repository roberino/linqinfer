using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Learning.Classification.NeuralNetworks
{
    internal class MaximumFitnessMultilayerNetworkTrainingStrategy<TClass, TInput> : IAsyncMultilayerNetworkTrainingStrategy<TClass, TInput> where TClass : IEquatable<TClass> where TInput : class
    {
        private const float _minRateOfChange = 0.001f;

        private readonly NetworkParameterCache _paramCache;
        private readonly Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<NetworkParameters>, double> _fitnessFunction;
        private readonly Func<IClassifierTrainingContext<NetworkParameters>, int, TimeSpan, bool> _haltingFunction;

        private double _currentLearningRate = 0.1;

        public MaximumFitnessMultilayerNetworkTrainingStrategy(
            float errorTolerance = 0.3f,
            Func<IFloatingPointFeatureExtractor<TInput>, IClassifierTrainingContext<NetworkParameters>, double> fitnessFunction = null,
            Func<IClassifierTrainingContext<NetworkParameters>, int, TimeSpan, bool> haltingFunction = null)
        {
            _paramCache = NetworkParameterCache.DefaultCache;

            _fitnessFunction = fitnessFunction ?? ClassifierFitnessFunctions.ErrorMinimisationFunction<TInput, TClass>();
            _haltingFunction = haltingFunction ?? ((c, i, t) => i > 500 || t > TimeSpan.FromSeconds(10));

            ErrorTolerance = errorTolerance;
            ParallelProcess = true;
        }

        public float ErrorTolerance { get; set; }

        public bool ParallelProcess { get; set; }

        public Task<IClassifierTrainingContext<NetworkParameters>> Train(ITrainingSet<TInput, TClass> trainingSet, Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> trainingContextFactory)
        {
            return Task<IClassifierTrainingContext<NetworkParameters>>.Factory.StartNew(() =>
            {
                return TrainInternal(trainingSet, trainingContextFactory);
            });
        }

        private IClassifierTrainingContext<NetworkParameters> TrainInternal(ITrainingSet<TInput, TClass> trainingSet, Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> trainingContextFactory)
        {
            var timer = new Stopwatch();

            var featureSet = trainingSet.FeaturePipeline;
            var pipelineFact = new PipelineFactory(trainingContextFactory, featureSet.FeatureExtractor.VectorSize, trainingSet.OutputMapper.VectorSize, _currentLearningRate);

            var networks = Activators
                .All()
                .Where(a => a.Name.StartsWith("Sig"))
                .SelectMany(a => pipelineFact.GeneratePipelines(a))
                .Concat(_paramCache.Get<TClass>(featureSet.VectorSize, trainingSet.OutputMapper.VectorSize).Take(1).Select(trainingContextFactory))
                .ToList();

            var nc = networks.Count;
            var onc = nc;
            var i = 0;

            timer.Start();

            while (!_haltingFunction(networks.FirstOrDefault(), i, timer.Elapsed))
            {
                if (networks.All(n => n.AverageError.HasValue && double.IsNaN(n.AverageError.Value)))
                {
                    if (_currentLearningRate > 0.05)
                    {
                        _currentLearningRate -= 0.05;

                        DebugOutput.Log("Reducing learning rate: " + _currentLearningRate);

                        return TrainInternal(trainingSet, trainingContextFactory);
                    }
                    else
                    {
                        break;
                    }
                }

                var unconverged = (i == 0) ? networks : networks.Where(n => !HasConverged(n)).ToList();

                DebugOutput.Log(networks);

                unconverged.AsParallel().ForAll(n => n.ResetError());

                foreach (var batch in trainingSet.ExtractTrainingVectorBatches())
                {
                    unconverged.AsParallel()
                        .WithDegreeOfParallelism(ParallelProcess ? Environment.ProcessorCount : 1).ForAll(n =>
                    {
                        foreach (var value in batch.RandomOrder())
                        {
                            n.Train(value.Input, value.TargetOutput);
                        }

                        n.IterationCounter++;
                    });
                }

                var somewhatTrained = networks.Where(n => n.IterationCounter >= 2).ToList();
                var notTrained = networks.Where(n => n.IterationCounter < 2).ToList();

                nc = Math.Max((somewhatTrained.Count / 2), 2);

                if (networks.Count > 3)
                    networks = notTrained
                        .Concat(somewhatTrained
                            .OrderByDescending(n => _fitnessFunction(featureSet.FeatureExtractor, n)).Take(Math.Min(onc, nc)))
                        .ToList();

                if (networks.All(n => HasConverged(n)))
                {
                    DebugOutput.Log("All networks converged");
                    break;
                }

                if (!somewhatTrained.Any())
                {
                    networks.Add(Breed(trainingContextFactory, networks[0], networks[1]));
                    networks.Add(Breed(trainingContextFactory, networks[1], networks[0]));
                }

                i++;
            }

            timer.Stop();

            DebugOutput.Log(networks);

            var bestSolution = networks.OrderByDescending(n => _fitnessFunction(featureSet.FeatureExtractor, n)).First();

            DebugOutput.Log("Best: {0}", bestSolution);

            _paramCache.Store<TClass>(bestSolution.Parameters, bestSolution.AverageError.GetValueOrDefault());

            return bestSolution;
        }

        private IClassifierTrainingContext<NetworkParameters> Breed(Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> trainingContextFactory, IClassifierTrainingContext<NetworkParameters> contextA, IClassifierTrainingContext<NetworkParameters> contextB)
        {
            var newParameters = contextA.Parameters.Breed(contextB.Parameters);

            DebugOutput.Log("Breed:{0} + {1} = {2}", contextA, contextB, newParameters);

            return trainingContextFactory(newParameters);
        }

        private bool HasConverged(IClassifierTrainingContext<NetworkParameters> context)
        {
            return
                (context.AverageError.HasValue &&
                    (double.IsNaN(context.AverageError.Value) ||
                    context.AverageError <= ErrorTolerance))
                    ||
                (context.RateOfErrorChange.HasValue &&
                    context.RateOfErrorChange < _minRateOfChange);
        }

        private class PipelineFactory
        {
            private readonly Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> _trainingContextFactory;
            private readonly NetworkParameterFactory _nwpf;

            public PipelineFactory(Func<NetworkParameters, IClassifierTrainingContext<NetworkParameters>> trainingContextFactory, int vectorSize, int outputSize, double learningRate = 0.1)
            {
                _trainingContextFactory = trainingContextFactory;
                _nwpf = new NetworkParameterFactory(vectorSize, outputSize, learningRate);
            }

            public IEnumerable<IClassifierTrainingContext<NetworkParameters>> GeneratePipelines(ActivatorFunc activator)
            {
                return _nwpf.GenerateParameters(activator).Select(_trainingContextFactory);
            }

            private static int[] L(params int[] n)
            {
                return n;
            }
        }
    }
}