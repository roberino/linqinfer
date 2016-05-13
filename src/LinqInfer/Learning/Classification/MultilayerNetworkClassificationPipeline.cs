﻿using LinqInfer.Learning.Features;
using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Learning.Classification
{
    internal class MultilayerNetworkClassificationPipeline<TClass, TInput> where TClass : IEquatable<TClass>
    {
        private readonly int _maxIterations;
        private readonly Config _config;
        private readonly NetworkParameterCache _paramCache;

        private Pipeline _pipeline;

        public MultilayerNetworkClassificationPipeline(
            IFeatureExtractor<TInput, double> featureExtractor,
            float errorTolerance = 0.3f,
            int maxIterations = 200,
            IOutputMapper<TClass> outputMapper = null) : this(Setup(featureExtractor, outputMapper, default(TInput)), errorTolerance, maxIterations)
        {
        }

        private MultilayerNetworkClassificationPipeline(Config config, float errorTolerance, int maxIterations) // : base(config.Trainer, config.Classifier, config.FeatureExtractor, config.NormalisingSample)
        {
            ErrorTolerance = errorTolerance;
            ParallelProcess = true;
            _config = config;
            _paramCache = NetworkParameterCache.DefaultCache;
            _maxIterations = maxIterations;
        }

        public float ErrorTolerance { get; set; }

        public bool ParallelProcess { get; set; }

        public double Train(IQueryable<TInput> trainingData, Expression<Func<TInput, TClass>> classifyingExpression)
        {
            int hiddenLayerFactor = 8;
            IList<TClass> outputs = null;

            if (_config.OutputMapper.VectorSize == 0)
            {
                outputs = trainingData.GroupBy(classifyingExpression).Select(o => o.Key).ToList();

                _config.OutputMapper.Initialise(outputs);
                _config.FeatureExtractor.NormaliseUsing(trainingData);
            }

            var networks = Activators
                .All()
                .SelectMany(a => GeneratePipelines(hiddenLayerFactor, outputs.Count, a))
                .Concat(_paramCache.Get<TClass>().Take(2).Select(Create))
                .ToList();

            var iterationReductionFactor = hiddenLayerFactor; // reduce by 1/iterationReductionFactor each iteration
            var classf = classifyingExpression.Compile();
            var nc = networks.Count;
            var trainingDataCount = trainingData.Count();
            var i = 0;

            while (i < _maxIterations)
            {
                var unconverged = (i == 0) ? networks : networks.Where(n => !n.HasConverged(trainingDataCount)).ToList();

                unconverged.AsParallel().ForAll(n => n.Instance.ResetError());

                foreach (var batch in trainingData.Chunk())
                {
                    unconverged.AsParallel().WithDegreeOfParallelism(ParallelProcess ? Environment.ProcessorCount : 1).ForAll(n =>
                    {
                        foreach (var value in batch)
                        {
                            n.Instance.Train(value, classf);
                        }
                    });
                }
                
                nc = networks.Count * (iterationReductionFactor - 1) / iterationReductionFactor;
                
                if (networks.Count > 3) networks = networks.OrderBy(n => n.Instance.Error).Take(nc).ToList();

                if (networks.All(n => n.HasConverged(trainingDataCount))) break;

                networks.Add(Breed(networks[0], networks[1]));

                i++;
            }

            _pipeline = networks.OrderBy(n => n.Instance.Error).First();

            Debugger.Log(_pipeline);

            var err = _pipeline.Instance.Error.GetValueOrDefault() / trainingDataCount;

            _paramCache.Store<TClass>(_pipeline.Parameters, err);

            return err;
        }

        public IEnumerable<ClassifyResult<TClass>> Classify(TInput obj)
        {
            if (_pipeline == null) throw new InvalidOperationException("Pipeline not trained");

            return _pipeline.Instance.Classify(obj);
        }

        private Pipeline Breed(Pipeline a, Pipeline b)
        {
            return Create(a.Parameters.Breed(b.Parameters));
        }

        private Pipeline Create(NetworkParameters newParams)
        {
            var network = new MultilayerNetwork(newParams);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample, false);
            
            return new Pipeline()
            {
                Instance = pipeline,
                Parameters = network.Parameters,
                ErrorTolerance = ErrorTolerance
            };
        }

        private IEnumerable<Pipeline> GeneratePipelines(int hiddenLayerFactor, int outputSize, ActivatorFunc activator)
        {
            return Enumerable.Range(0, hiddenLayerFactor)
                .Select(n => SetupNetwork(n, outputSize, activator));
        }

        private Pipeline SetupNetwork(int hiddenLayerFactor, int outputSize, ActivatorFunc activator)
        {
            var network = new MultilayerNetwork(_config.FeatureExtractor.VectorSize, activator);
            var bpa = new BackPropagationLearning(network);
            var learningAdapter = new AssistedLearningAdapter<TClass>(bpa, _config.OutputMapper);
            var classifier = new MultilayerNetworkClassifier<TClass>(network, _config.OutputMapper.Map);

            var pipeline = new ClassificationPipeline<TClass, TInput, double>(learningAdapter, classifier, _config.FeatureExtractor, _config.NormalisingSample, false);

            network.Initialise(_config.FeatureExtractor.VectorSize, _config.FeatureExtractor.VectorSize / 2 * hiddenLayerFactor, outputSize);

            return new Pipeline()
            {
                Instance = pipeline,
                Parameters = network.Parameters,
                ErrorTolerance = ErrorTolerance
            };
        }

        private static Config Setup(
            IFeatureExtractor<TInput, double> featureExtractor,
            IOutputMapper<TClass> outputMapper,
            TInput normalisingSample)
        {
            if (outputMapper == null) outputMapper = new OutputMapper<TClass>();
            
            return new Config()
            {
                NormalisingSample = normalisingSample,
                FeatureExtractor = featureExtractor,
                OutputMapper = outputMapper
            };
        }

        private class Pipeline
        {
            public ClassificationPipeline<TClass, TInput, double> Instance { get; set; }
            public NetworkParameters Parameters { get; set; }
            public float ErrorTolerance { get; set; }

            public bool HasConverged(int trainingDataCount)
            {
                return Instance.Error.HasValue && (Instance.Error / trainingDataCount) < ErrorTolerance;
            }

            public override string ToString()
            {
                return string.Format("error:{0} parameters:{1}", Instance.Error, Parameters);
            }
        }

        private class Config
        {
            public  TInput NormalisingSample { get; set; }
            public IOutputMapper<TClass> OutputMapper { get; set; }
            public IFeatureExtractor<TInput, double> FeatureExtractor { get; set; }
        }
    }
}